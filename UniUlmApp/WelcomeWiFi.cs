using System;
using System.Net;
using System.Text;

namespace UniUlmApp
{
    public class WelcomeWiFi
    {
        public event Action finishedLogin;
        public event Action needsLogin;
        public event Action<string> loginError;

        const string loginUrl = "https://welcome.uni-ulm.de/capo/";

        public WelcomeWiFi()
        {
            // avoid null checks
            this.finishedLogin += () => { };
            this.needsLogin += () => { };
            this.loginError += (_) => { };
        }

        public void checkConnection()
        {
            var testCode = DateTime.Now.Millisecond;
            var testurl = "http://tvprogramm.somee.com/onlinecheck.php?test=" + testCode;
            var wc = new WebClient();

            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(testUrlDownloadComplete);
            wc.DownloadStringAsync(new Uri(testurl), testCode.ToString());
        }

        public void login(string user, string pass)
        {
            var req = HttpWebRequest.Create(loginUrl);
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            string postData = "username=" + user + "&password=" + pass + "&login=start+network+access";
            var postDataBytes = System.Text.UTF8Encoding.UTF8.GetBytes(postData);
            req.BeginGetRequestStream(e1 =>
                {
                    var stream = req.EndGetRequestStream(e1);
                    stream.Write(postDataBytes, 0, postDataBytes.Length);
                    stream.Close();
                    req.BeginGetResponse(e2 =>
                        {
                            var respStream = req.EndGetResponse(e2).GetResponseStream();
                            var buffer = new byte[512];
                            var sb = new StringBuilder();
                            int len = 0;
                            while ((len = respStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                sb.Append(System.Text.UTF8Encoding.UTF8.GetString(buffer, 0, len));
                            }
                            if (sb.ToString().Contains("Network access allowed"))
                            {
                                this.finishedLogin();
                            }
                            else
                            {
                                this.loginError("Benutzer oder Passwort vermutlich nicht korrekt.");
                            }
                        }, null);
                }, null);
        }

        void testUrlDownloadComplete(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                this.loginError("Konnte keine Verbindung aufbauen.");
                return;
            }
            /* Parse this
             * <input type="hidden" name="mac" value="00:22:fb:a0:eb:92" /><input
              type="hidden" name="token" value="$1$99635988$2poazjIUzy0t4ti3PmqId." /><input
              type="hidden" name="redirect" value="http://www.uni-ulm.de/mensaplan/mensaplan.xml" /><input
              type="hidden" name="gateway" value="" /><input
              type="hidden" name="timeout" value="28800" />
                    */
            if (e.Result.Contains(e.UserState.ToString()) == false)
            {
                this.needsLogin();
            }
            else // if we are not on the login page and there was no error, we are probably already logged in
            {
                this.finishedLogin();
            }
        }
    }
}
