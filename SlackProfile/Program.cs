using SlackProfile.Helpers;
using SlackProfile.Items.SetUsersProfile.Request;
using SlackProfile.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SlackProfile
{
    class Program
    {
        public const string CLIENT_ID = "3507541088166.3969218284066";
        public const string SCOPE = "users.profile:read" + " " + "users.profile:write";

        static void Main(string[] args)
        {
            Run().GetAwaiter().GetResult();
        }

        public static async Task Run()
        {
            //토큰 확인
            var tokenFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "token.txt");
            var token = File.ReadAllText(tokenFilePath).Trim();

            if (string.IsNullOrWhiteSpace(token) || !token.StartsWith("xoxp-"))
            {
                MessageBox.Show("" +
                    "프로필 변경을 하기 위해서는 슬랙 로그인이 필요합니다.\n" +
                    "슬랙 로그인 후 화면에 나타나는 토큰을 token.txt에 붙여넣기 후 프로그램을 다시 실행하세요.\n" +
                    "\n" +
                    "확인을 누르면 로그인 페이지로 이동합니다.");

                Process.Start($"https://slack.com/oauth/authorize?client_id={CLIENT_ID}&scope={SCOPE}?state={state}");
                return;
            }

            //temp state 있는지 확인

            //state 생성 후 로그인

#warning state 처리 필요
            var state = Guid.NewGuid().ToString();

            //준비
            var isRemoteSession = SystemInformation.TerminalServerSession;
            var slackAPI = new SlackAPI(token);
            
            //기존 프로필 확인
            var userProfile = await slackAPI.GetUsersProfileAsync();

            //변경 프로필 생성
            var profile = new Profile
            {
                RealName = new Func<string>(() =>
                {
                    var name = userProfile.Profile.RealName;

                    //이미 지나간 표시 제거 (최근 3일), ex) (8/24 반반차), (8/24 슈가), ...
                    for (int i = 1; i <= 3; i++)
                    {
                        var date = DateTime.Now.AddDays(i * -1);

                        name = Regex.Replace(name, $@"\(0?{date.Month}/0?{date.Day}.*?\)", "");
                    }

                    //재택 문자 제거
                    name = name.Replace("(재택)", string.Empty);

                    //공백 제거
                    name = name.Replace("  ", " ");

                    //Trim
                    name = name.Trim();

                    //재택 문구 추가
                    if (isRemoteSession)
                    {
                        name = $"(재택){name}";
                    }

                    return name;
                }).Invoke(),
                StatusText = isRemoteSession ? "재택근무 중" : string.Empty,
                StatusEmoji = isRemoteSession ? ":house_with_garden:" : string.Empty,
                StatusExpiration = isRemoteSession ? DateTimeHelper.GetTodayEndUnixTimestamp() : 0
            };
            
            //프로필 변경
            var response = await slackAPI.SetUsersProfileAsync(profile);

            //출력
            //Console.WriteLine($"name: {response.Profile.RealName} / isRemoteSession: {isRemoteSession}");
            //await Task.Delay(2000);

            return;
        }
    }
}
