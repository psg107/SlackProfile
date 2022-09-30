using DeviceId;
using NativeWifi;
using SlackProfile.Helpers;
using SlackProfile.Items.SetUsersProfile.Request;
using SlackProfile.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SlackProfile
{
    class Program
    {
        public const string CLIENT_ID           = "3507541088166.3969218284066";
        public const string SCOPE               = "users.profile:read" + " " + "users.profile:write";
        public const string TOKEN_FILE_NAME     = "token.txt";
        public const string EXE_FILE_NAME       = "SlackProfile.exe";
        public const string TOKEN_KEY           = "Token";
        public const string DEVICE_KEY          = "DeviceId";
        public const string TASK_SCHEDULER_PATH = "SlackProfile";

        public static readonly Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);

        static void Main(string[] args)
        {
            System.AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var exception = e.ExceptionObject as Exception;

                Logger.WriteLine($"[UnhandledException] {exception.ToString()}");
            };

            Run().GetAwaiter().GetResult();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static async Task Run()
        {
            Logger.WriteLine($"시작");

            var homeSSIDNames = config.AppSettings.Settings["HOME_SSID_NAMES"].Value.Split(';');

            //토큰 확인
            var tokenFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TOKEN_FILE_NAME);
            var token = await GetToken(tokenFilePath);
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            //작업스케줄러 확인
            RegisterTaskScheduler();

            //준비
            var isRemoteSession = IsRemote(homeSSIDNames);
            var slackAPI = new SlackAPI(token);

            //기존 프로필 확인
            var userProfile = await slackAPI.GetUsersProfileAsync();
            Logger.WriteLine($"원격세션: {isRemoteSession}");
            Logger.WriteLine($"변경 전: RealName:'{userProfile.Profile.RealName}' / StatusText:'{userProfile.Profile.StatusText}' / StatusEmoji:'{userProfile.Profile.StatusEmoji}' StatusExpiration:'{userProfile.Profile.StatusExpiration}'");

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
                StatusEmoji = isRemoteSession ? ":정원이_있는_집:" : string.Empty,
                StatusExpiration = isRemoteSession ? DateTimeHelper.GetTodayEndUnixTimestamp() : 0
            };

            //프로필 변경
            var response = await slackAPI.SetUsersProfileAsync(profile);

            //로깅
            Logger.WriteLine($"변경 후: RealName:'{profile.RealName}' / StatusText:'{profile.StatusText}' / StatusEmoji:'{profile.StatusEmoji}' StatusExpiration:'{profile.StatusExpiration}'");
            Logger.WriteLine("종료");
            Logger.WriteLine("--------------------------------------------------------");

            return;
        }

        /// <summary>
        /// 작업스케줄러 등록
        /// </summary>
        private static void RegisterTaskScheduler()
        {
            using (Microsoft.Win32.TaskScheduler.TaskService ts = new Microsoft.Win32.TaskScheduler.TaskService())
            {
                var slackProfileTask = ts.GetTask(TASK_SCHEDULER_PATH);

                //이미 작업스케줄러가 존재하는 경우
                if (slackProfileTask != null)
                {
                    var executeAction = slackProfileTask.Definition.Actions.Where(x => x.GetType() == typeof(Microsoft.Win32.TaskScheduler.ExecAction)).FirstOrDefault() as Microsoft.Win32.TaskScheduler.ExecAction;

                    //동작이 없으면 추가
                    if (executeAction == null)
                    {
                        slackProfileTask.Definition.Actions.Add(new Microsoft.Win32.TaskScheduler.ExecAction(EXE_FILE_NAME, arguments: null, workingDirectory: AppDomain.CurrentDomain.BaseDirectory));
                        slackProfileTask.RegisterChanges();
                        return;
                    }

                    //시작위치가 다르면 변경
                    if (executeAction.WorkingDirectory != AppDomain.CurrentDomain.BaseDirectory)
                    {
                        executeAction.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                        slackProfileTask.RegisterChanges();
                        return;
                    }

                    return;
                }

                var userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

                Microsoft.Win32.TaskScheduler.TaskDefinition td = ts.NewTask();
                td.RegistrationInfo.Description = "Slack Profile";

                //트리거
                td.Triggers.Add(new Microsoft.Win32.TaskScheduler.LogonTrigger { UserId = userName });
                td.Triggers.Add(new Microsoft.Win32.TaskScheduler.SessionStateChangeTrigger { StateChange = Microsoft.Win32.TaskScheduler.TaskSessionStateChangeType.SessionUnlock, UserId = userName });

                //실행
                td.Actions.Add(new Microsoft.Win32.TaskScheduler.ExecAction(EXE_FILE_NAME, arguments: null, workingDirectory: AppDomain.CurrentDomain.BaseDirectory));

                //등록
                ts.RootFolder.RegisterTaskDefinition(TASK_SCHEDULER_PATH, td, Microsoft.Win32.TaskScheduler.TaskCreation.CreateOrUpdate, userId: userName, password: null, logonType: Microsoft.Win32.TaskScheduler.TaskLogonType.InteractiveToken);
            }
        }

        /// <summary>
        /// 토큰 가져오기
        /// </summary>
        /// <returns></returns>
        private static async Task<string> GetToken(string tokenFilePath)
        {
            var savedDeviceId = config.AppSettings.Settings[DEVICE_KEY]?.Value;
            var deviceId = new DeviceIdBuilder().AddMachineName().AddMacAddress().AddUserName().ToString();

            //저장된 디바이스 정보가 없거나 다른 경우
            if (savedDeviceId == null || savedDeviceId != deviceId)
            {
                savedDeviceId = deviceId;

                config.AppSettings.Settings.Remove(TOKEN_KEY);
                config.AppSettings.Settings.Remove(DEVICE_KEY);
                config.AppSettings.Settings.Add(DEVICE_KEY, deviceId);
                config.Save(ConfigurationSaveMode.Minimal);
            }

            //로컬에 토큰이 있는 경우
            var token = config.AppSettings.Settings[TOKEN_KEY]?.Value;
            if (token?.StartsWith("xoxp-") == true)
            {
                Logger.WriteLine("토큰 확인");
                return token;
            }

            //로컬에 토큰이 없으나 서버에는 있는 경우
            var downloadToken = await new HttpClient().GetStringAsync($"https://nowwaitingsearch.azurewebsites.net/oauth/token?state={deviceId}");
            if (!string.IsNullOrWhiteSpace(downloadToken))
            {
                Logger.WriteLine($"토큰 다운로드");
                config.AppSettings.Settings.Add(TOKEN_KEY, downloadToken);
                config.Save(ConfigurationSaveMode.Minimal);
                return downloadToken;
            }

            //로컬/서버에 토큰이 없는 경우 생성
            Logger.WriteLine($"로그인");
            MessageBox.Show("" +
                "프로필 변경을 하기 위해서는 슬랙 로그인이 필요합니다.\n" +
                "확인을 누르면 로그인 페이지로 이동합니다.", "SlackProfile");

            Process.Start($"https://slack.com/oauth/authorize?client_id={CLIENT_ID}&scope={SCOPE}&state={deviceId}");
            return string.Empty;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        private static bool IsRemote(IEnumerable<string> homeSSIDNames)
        {
            var isRemoteSession = SystemInformation.TerminalServerSession;
            if (isRemoteSession)
            {
                return true;
            }

            var connectedSSIDs = GetConnectedSSIDs();
            var availableSSIDs = GetAvailableSSIDs();

            if (connectedSSIDs.Any(x => homeSSIDNames.Contains(x)))
            {
                return true;
            }
            if (availableSSIDs.Any(x => homeSSIDNames.Contains(x)))
            {
                return true;
            }
            return false;
        }
        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        private static List<string> GetConnectedSSIDs()
        {
            List<string> connectedSsids = new List<string>();

            WlanClient client = new WlanClient();
            foreach (WlanClient.WlanInterface wlanInterface in client.Interfaces)
            {
                Wlan.Dot11Ssid ssid = wlanInterface.CurrentConnection.wlanAssociationAttributes.dot11Ssid;
                connectedSsids.Add(new string(Encoding.ASCII.GetChars(ssid.SSID, 0, (int)ssid.SSIDLength)));
            }

            return connectedSsids;
        }
        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        private static List<string> GetAvailableSSIDs()
        {
            List<string> availableSsids = new List<string>();

            WlanClient client = new WlanClient();
            foreach (WlanClient.WlanInterface wlanInterface in client.Interfaces)
            {
                Wlan.WlanAvailableNetwork[] networks = wlanInterface.GetAvailableNetworkList(0);
                foreach (Wlan.WlanAvailableNetwork network in networks)
                {
                    Wlan.Dot11Ssid ssid = network.dot11Ssid;
                    string networkname = Encoding.ASCII.GetString(ssid.SSID, 0, (int)ssid.SSIDLength);
                    if (!string.IsNullOrEmpty(networkname))
                    {
                        availableSsids.Add(networkname.ToString());
                    }
                }
            }

            return availableSsids;
        }
    }
}
