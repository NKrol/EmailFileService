using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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

namespace EmailFileServiceWpfApp2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await Do();
        }

        private async Task Do()
        {
            using (var client = new HttpClient())
            {
                var register = "{\r\n    \"Email\": \"nkrol3@gmail.com\",\r\n    \"Password\": \"password\",\r\n    \"ConfirmedPassword\": \"password\"\r\n}";
                var login = "{\r\n    \"Email\": \"nkrol5@gmail.com\",\r\n    \"Password\": \"password\"\r\n}";

                var registerContent = new StringContent(register, Encoding.UTF8, "application/json");
                var loginContent = new StringContent(login, Encoding.UTF8, "application/json");

                var responseRegister = await client.PostAsync("http://localhost:5000/api/account/register", registerContent);

                var responseLogin = await client.PostAsync("http://localhost:5000/api/account/login", loginContent);

                var loginContentResponse = responseLogin;

                client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse("Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjQiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJua3JvbDVAZ21haWwuY29tIiwiZXhwIjoxNjMyMTUzNzc5LCJpc3MiOiJodHRwOi8vZmlsZXNlcnZpY2UuY29tIiwiYXVkIjoiaHR0cDovL2ZpbGVzZXJ2aWNlLmNvbSJ9.QigtB4X8iQnGz-Fd_TkhlxJomUqKkvTpxiXDSQapu2U");

                var responseFile = await client.GetAsync("http://localhost:5000/api/file/myfolders");
            }
        }
    }
}
