using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using userRegistrationFormMVC.Data;

namespace userRegistrationFormMVC.Controllers
{
    /// <summary>
    /// ユーザー情報操作の管理を担当するコントローラー
    /// ユーザー登録の CRUD (作成、読み取り、更新、削除) 操作を処理します
    /// </summary>
    [Authorize]
    public class UserInfoesController : Controller
    {
        // ユーザー情報にアクセスするためのデータベースコンテキスト
        private readonly ApplicationDbContext _context;
        // コントローラのアクティビティを追跡および記録するためのロガー
        private readonly ILogger<UserInfoesController> _logger;

        /// <summary>
        /// データベースコンテキストとロガーを使用してコントローラを初期化するコンストラクタ
        /// </summary>
        /// <param name="context">ユーザー情報のデータベースコンテキスト</param>
        /// <param name="logger">コントローラのアクティビティを追跡するためのロガー</param>
        public UserInfoesController(ApplicationDbContext context, ILogger<UserInfoesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// すべてのユーザーのリストを表示するGETアクション
        /// </summary>
        /// <returns>ユーザーリストを表示</returns>
        public async Task<IActionResult> Index()
        {
            // ユーザーリストへのユーザーアクセスを記録する
            _logger.LogInformation($"ユーザー '{User.Identity.Name}' は、{DateTime.Now} からユーザー リストにアクセスしました。");

            return View(await _context.UserInfo.ToListAsync());
        }

        /// <summary>
        ///ID で特定のユーザーの詳細を表示する GET アクション
        /// </summary>
        /// <param name="id">詳細を取得するためのユーザーID</param>
        /// <returns>ユーザーの詳細を表示するか、ユーザーが存在しない場合は NotFound を表示します。</returns>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning($"ユーザー '{User.Identity.Name}' が {DateTime.Now} に null ユーザー ID の詳細にアクセスしようとしました");
                return NotFound();
            }

            var userInfo = await _context.UserInfo
                .FirstOrDefaultAsync(m => m.Id == id);
            if (userInfo == null)
            {
                _logger.LogWarning($"ユーザー '{User.Identity.Name}' は、{DateTime.Now} に存在しないユーザー ID {id} の詳細にアクセスしようとしました。");
                return NotFound();
            }
            _logger.LogInformation($"ユーザー '{User.Identity.Name}' は、{DateTime.Now} に ID {id} の詳細を表示しました。");
            return View(userInfo);
        }

        /// <summary>
        /// ユーザー作成フォームを表示するGETアクション
        /// </summary>
        /// <returns>新しいユーザーを作成するためのビュー</returns>
        public IActionResult Create()
        {
            // ユーザーページを作成するためのアクセスをログに記録する
            _logger.LogInformation($"ユーザー '{User.Identity.Name}' が {DateTime.Now} にページをアクセスしました");
            return View();
        }

        /// <summary>
        /// 最初のフォーム送信を処理するPOSTアクション
        /// 現在は送信されたデータを含むフォームビューを返すだけです
        /// </summary>
        /// <param name="userInfo">フォームから送信されたユーザー情報</param>
        /// <returns>送信されたユーザーデータを使用してビューを作成する</returns>
        [HttpPost]
        public IActionResult Create(UserInfo userInfo)
        {
            // 最初のフォーム送信試行をログに記録する
            _logger.LogInformation($"ユーザー '{User.Identity.Name}' は、{DateTime.Now} に送信されたフォームをロールバックしました.");
            return View("Create", userInfo);
        }

        /// <summary>
        /// 最終送信前にユーザー情報を確認するための POST アクション
        /// ユーザー入力を検証し、確認ページにリダイレクトします
        /// </summary>
        /// <param name="userInfo">確認が必要なユーザー情報</param>
        /// <returns>検証に失敗した場合は確認ビューまたは作成ビューに戻る</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Confirm([Bind("Id,Name,Kana,Gender,Birthdate,PhoneNumber,Email,LoginId,Password,ConfirmPassword")] UserInfo userInfo)
        {
            // ログ確認ページへのアクセス
            _logger.LogInformation($"ユーザー '{User.Identity.Name}' の確認ページが {DateTime.Now} にアクセスしました。");

            //Model stateとカスタム検証を確認する
            if (!ModelState.IsValid || !ValidateUserInfo(userInfo))
            {
                _logger.LogWarning($"{DateTime.Now} にユーザー情報の検証に失敗しました。 エラー: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                return View("Create", userInfo);
            }

            return View("Confirmation", userInfo);
        }

        /// <summary>
        ///ユーザー情報をデータベースに保存するPOSTアクション
        ///検証を実行し、生のSQLを使用してユーザーデータを挿入します
        /// </summary>
        /// <param name="userInfo">保存するユーザー情報</param>
        /// <returns>検証に失敗した場合は、ユーザーリストにリダイレクトするか、確認ページに戻ります。</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save([Bind("Id,Name,Kana,Gender,Birthdate,PhoneNumber,Email,LoginId,Password")] UserInfo userInfo)
        {
            // ユーザーの保存試行の詳細なログ
            _logger.LogInformation($"ユーザー '{User.Identity.Name}' が {DateTime.Now} に情報を保存しようとしています。");

            //ユーザー情報を挿入するための生のSQLクエリ
            string query = "INSERT INTO UserInfo (Name, Kana, Gender, Birthdate, PhoneNumber, Email, LoginId, Password) " +
                           "VALUES (@Name, @Kana, @Gender, @Birthdate, @PhoneNumber, @Email, @LoginId, @Password)";

            //ハードコードされた接続文字列
            string connectionString = "Server=(localdb)\\mssqllocaldb;Database=aspnet-userRegistrationFormMVC-aeca3ef4-437c-4d58-8e68-3c8d23dd87a1;Trusted_Connection=True;MultipleActiveResultSets=true";

            try
            {
                //データベース接続を開き、挿入を実行する
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(query, connection))
                    {
                        // SQLインジェクションを防ぐためのパラメータを追加する
                        command.Parameters.AddWithValue("@Name", userInfo.Name);
                        command.Parameters.AddWithValue("@Kana", userInfo.Kana);
                        command.Parameters.AddWithValue("@Gender", userInfo.Gender);
                        command.Parameters.AddWithValue("@Birthdate", userInfo.Birthdate);
                        command.Parameters.AddWithValue("@PhoneNumber", userInfo.PhoneNumber);
                        command.Parameters.AddWithValue("@Email", userInfo.Email);
                        command.Parameters.AddWithValue("@LoginId", userInfo.LoginId);
                        command.Parameters.AddWithValue("@Password", userInfo.Password);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                //ユーザー作成成功のログ
                _logger.LogInformation($"ユーザー '{User.Identity.Name}' が {DateTime.Now} にユーザー情報を正常に作成しました。");

                TempData["SuccessMessage"] = "ユーザー情報が正常に保存されました。";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // ユーザー作成中に発生した例外をログに記録する
                _logger.LogError(ex, $"{DateTime.Now} にユーザー情報を保存中にエラーが発生しました。 エラー: {ex.Message}");
                // 次のページに表示される成功メッセージを設定します
                TempData["ErrorMessage"] = "ユーザー情報の保存中にエラーが発生しました。";
                return View("Confirmation", userInfo);
            }
        }

        /// <summary>
        /// 特定のユーザーの編集フォームを表示するGETアクション
        /// </summary>
        /// <param name="id">編集するユーザーID</param>
        /// <returns>ユーザー情報または NotFound を含むビューを編集する</returns>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning($"{DateTime.Now} に null ユーザー ID を編集しようとしました。");
                return NotFound();
            }
            var userInfo = await _context.UserInfo.FindAsync(id);
            if (userInfo == null)
            {
                _logger.LogWarning($"{DateTime.Now} に存在しないユーザー ID {id} を編集しようとしました。");
                return NotFound();
            }
            _logger.LogInformation($"ユーザー '{User.Identity.Name}' が {DateTime.Now} に ID {id} の編集ページにアクセスしました。");
            return View(userInfo);
        }

        /// <summary>
        /// データベース内のユーザー情報を更新するためのPOSTアクション
        ///生のSQLを使用してユーザーの詳細を更新します
        /// </summary>
        /// <param name="id">更新するユーザーID</param>
        /// <param name="userInfo">更新しましたユーザー情報</param>
        /// <returns>検証に失敗した場合は、ユーザーリストにリダイレクトするか、確認ページに戻ります。</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Kana,Gender,Birthdate,PhoneNumber,Email,Password")] UserInfo userInfo)
        {
            _logger.LogInformation($"ユーザー '{User.Identity.Name}' が {DateTime.Now} に情報を編集しようとしています。ID: {id}。");
            if (id != userInfo.Id)
            {
                _logger.LogWarning($"{DateTime.Now} の編集中にユーザー情報 ID が一致しません。要求された ID: {id}。");
                return NotFound();
            }

            // ユーザー情報を検証
            if (!ValidateUserInfo(userInfo))
            {
                _logger.LogWarning($"{DateTime.Now} にユーザー編集の検証に失敗しました。ユーザー ID: {id}");
                return View("Edit", userInfo);
            }

            // ユーザー情報を更新するための生の SQL クエリ
            string query = "UPDATE UserInfo SET " +
                           "Name = @Name, " +
                           "Kana = @Kana, " +
                           "Gender = @Gender, " +
                           "Birthdate = @Birthdate, " +
                           "PhoneNumber = @PhoneNumber, " +
                           "Email = @Email, " +
                           "Password = @Password " +
                           "WHERE Id = @Id";

            // ハードコードされた接続文字列
            string connectionString = "Server=(localdb)\\mssqllocaldb;Database=aspnet-userRegistrationFormMVC-aeca3ef4-437c-4d58-8e68-3c8d23dd87a1;Trusted_Connection=True;MultipleActiveResultSets=true";
            try
            {
                // データベース接続を開き、更新を実行する
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(query, connection))
                    {
                        //SQLインジェクションを防ぐためのパラメータを追加する
                        command.Parameters.AddWithValue("@Id", userInfo.Id);
                        command.Parameters.AddWithValue("@Name", userInfo.Name);
                        command.Parameters.AddWithValue("@Kana", userInfo.Kana);
                        command.Parameters.AddWithValue("@Gender", userInfo.Gender);
                        command.Parameters.AddWithValue("@Birthdate", userInfo.Birthdate);
                        command.Parameters.AddWithValue("@PhoneNumber", userInfo.PhoneNumber);
                        command.Parameters.AddWithValue("@Email", userInfo.Email);
                        command.Parameters.AddWithValue("@Password", userInfo.Password);

                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected == 0)
                        {
                            _logger.LogWarning($"{DateTime.Now} に情報 ID {id} の行は更新されませんでした");
                            return NotFound();
                        }
                    }
                }

                _logger.LogInformation($"ユーザー '{User.Identity.Name}' が {DateTime.Now} に情報を正常に編集しました。 ID: {id}。");
                //次のページに表示される成功メッセージを設定します
                TempData["SuccessMessage"] = "ユーザー情報が正常に編集されました。";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now} にユーザーを編集中にエラーが発生しました. ID: {id}, エラー: {ex.Message}");
                TempData["ErrorMessage"] = "ユーザー情報の編集中にエラーが発生しました。";
                return View("Edit", userInfo);
            }
        }


        /// <summary>
        /// ユーザーの削除確認ページを表示する GET アクション
        /// </summary>
        /// <param name="id">削除するユーザーID</param>
        /// <returns>確認ビューまたは NotFound を削除する</returns>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning($"{DateTime.Now} に null ID を削除しようとしました。");
                return NotFound();
            }

            var userInfo = await _context.UserInfo
                .FirstOrDefaultAsync(m => m.Id == id);
            if (userInfo == null)
            {
                _logger.LogWarning($"存在しない情報 ID {id} を {DateTime.Now} に削除しようとしました。");
                return NotFound();
            }
            _logger.LogInformation($"ユーザー '{User.Identity.Name}' が、{DateTime.Now} に情報 ID {id} の削除確認ページにアクセスしました。");
            return View(userInfo);
        }

        /// <summary>
        ///データベースからユーザーを確認して削除するPOSTアクション
        /// </summary>
        /// <param name="id">削除するユーザーID</param>
        /// <returns>ユーザーリストにリダイレクト</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            _logger.LogInformation($"{DateTime.Now} の情報を削除しようとしています。");
            var userInfo = await _context.UserInfo.FindAsync(id);
            if (userInfo != null)
            {
                _context.UserInfo.Remove(userInfo);
            }
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"ユーザー '{User.Identity.Name}' は {DateTime.Now} に正常に削除されました. ID: {id}");
                TempData["SuccessMessage"] = "ユーザー情報が正常に削除されました。";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now} の情報の削除中にエラーが発生しました。 ID: {id}, Error: {ex.Message}");
                TempData["ErrorMessage"] = "ユーザー情報の削除中にエラーが発生しました。";
                return RedirectToAction(nameof(Index));
            }
        }

        //// <summary>
        /// 一意のログインIDを検証するプライベートメソッド
        /// ログインIDがデータベースに既に存在するかどうかを確認し、生年月日が有効かどうかも確認
        /// </summary>
        /// <param name="userInfo">検証するユーザー情報</param>
        /// <returns>ユーザー情報が有効かどうかを示すブール値</returns>
        private bool ValidateUserInfo(UserInfo userInfo)
        {
            bool isValid = true;

            // Check for duplicate LoginId
            if (_context.UserInfo.Any(e => e.LoginId == userInfo.LoginId))
            {
                ModelState.AddModelError("LoginId", "このログインIDはすでに使用されています。別のIDを入力してください。");
                _logger.LogWarning($"{DateTime.Now} にログイン ID の検証に失敗しました. 重複ログインID: {userInfo.LoginId}");
                isValid = false;
            }

            // Validate Birthdate
            if (userInfo.Birthdate > DateTime.Today)
            {
                ModelState.AddModelError("Birthdate", "生年月日は現在の日付以前である必要があります。");
                _logger.LogWarning($"{DateTime.Now} に生年月日の検証に失敗しました. 無効な生年月日: {userInfo.Birthdate}");
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// ID でユーザーが存在するかどうかを確認するプライベート メソッド
        /// </summary>
        /// <param name="id">確認するユーザーID</param>
        /// <returns>ユーザーが存在するかどうかを示すブール値</returns>
        private bool UserInfoExists(int id)
        {
            return _context.UserInfo.Any(e => e.Id == id);
        }
    }
}