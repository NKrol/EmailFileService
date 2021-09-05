using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EmailFileServiceWpfApp
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void ButtonStart_OnClick(object sender, RoutedEventArgs e)
        {
            var token = "";
            using (var client = new HttpClient())
            {
                var jsonObject = "{\r\n   \"Email\" : \"nkrol5@gmail.com\",\r\n    \"Password\" : \"password\"\r\n}";

                var content = new StringContent(jsonObject, Encoding.UTF8, "application/json");

                var registerObjcet =
                    "{\r\n    \"Email\": \"nkrol5@gmail.com\",\r\n    \"Password\": \"password\",\r\n    \"ConfirmedPassword\": \"password\"\r\n}";

                var registerContent = new StringContent(registerObjcet, Encoding.UTF8, "application/json");

                var result = client.PostAsync("http://localhost:5000/api/account/register", registerContent).Result;

                var resultLogin = client.PostAsync("http://localhost:5000/api/account/login", content).Result;

                var stringa = result.Headers;
                var stringb = resultLogin.Content;
                token = stringb.ReadAsStringAsync().Result;
                var stringc = result.ReasonPhrase;
                var stringd = result.RequestMessage;
                var stringe = result.StatusCode;
                var stringf = result.ToString();

                //client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse("Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjEiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJua3JvbEBnbWFpbC5jb20iLCJleHAiOjE2MzIxNDUyNjUsImlzcyI6Imh0dHA6Ly9maWxlc2VydmljZS5jb20iLCJhdWQiOiJodHRwOi8vZmlsZXNlcnZpY2UuY29tIn0.PA4fYAInmgw6mHYLwLHR2CduYI28NU1YlHei3INvF3o");

            }

            using (var clientA = new HttpClient())
            {
                
                clientA.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {token}");

                Task<ShowMyFolders> response = await clientA.GetAsync("http://localhost:5000/api/file/myfolders");
            }
        }
    }
}
