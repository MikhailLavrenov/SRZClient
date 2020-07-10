using SrzClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace WebSrzClientExample
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual bool SetProperty<T>(ref T storage, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;

            storage = value;
            RaisePropertyChanged(propertyName);

            return true;
        }

        protected virtual bool SetProperty<T>(ref T storage, T value, Action onChanged, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;

            storage = value;
            onChanged?.Invoke();
            RaisePropertyChanged(propertyName);

            return true;
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            PropertyChanged?.Invoke(this, args);
        }

        string address = "10.1.0.202";
        public string Address { get => address; set => SetProperty(ref address, value, nameof(Address)); }

        string proxyAddress;
        public string ProxyAddress { get => proxyAddress; set => SetProperty(ref proxyAddress, value, nameof(ProxyAddress)); }

        ushort? proxyPort;
        public ushort? ProxyPort { get => proxyPort; set => SetProperty(ref proxyPort, value, nameof(ProxyPort)); }

        string login;
        public string Login { get => login; set => SetProperty(ref login, value, nameof(Login)); }

        string password;
        public string Password { get => password; set => SetProperty(ref password, value, nameof(Password)); }

        bool isAuthorized;
        public bool IsAuthorized { get => isAuthorized; set => SetProperty(ref isAuthorized, value, nameof(IsAuthorized)); }

        string enp;
        public string Enp { get => enp; set => SetProperty(ref enp, value, nameof(Enp)); }

        string snils;
        public string Snils { get => snils; set => SetProperty(ref snils, value, nameof(Snils)); }

        string surname;
        public string Surname { get => surname; set => SetProperty(ref surname, value, nameof(Surname)); }

        string firstName;
        public string FirstName { get => firstName; set => SetProperty(ref firstName, value, nameof(FirstName)); }

        string patronymic;
        public string Patronymic { get => patronymic; set => SetProperty(ref patronymic, value, nameof(Patronymic)); }

        string birthdate;
        public string Birthdate { get => birthdate; set => SetProperty(ref birthdate, value, nameof(Birthdate)); }

        string person;
        public string Person { get => person; set => SetProperty(ref person, value, nameof(Person)); }

        string personInfo;
        public string PersonInfo { get => personInfo; set => SetProperty(ref personInfo, value, nameof(PersonInfo)); }

        string errorMessage;
        public string ErrorMessage { get => errorMessage; set => SetProperty(ref errorMessage, value, nameof(ErrorMessage)); }

        WebSrzClient client;


        public MainWindow()
        {
            InitializeComponent();
        }

        private void ButtonAuthorizeClick(object sender, RoutedEventArgs e)
        {
            try
            {
                client = new WebSrzClient(Address, ProxyAddress, ProxyPort);
                client.Authorize(Login, Password);

                IsAuthorized = client.IsAuthorized;
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            { ErrorMessage = ex.Message; }
        }

        private void ButtonClearClick(object sender, RoutedEventArgs e)
        {
            Enp = default;
            Snils = default;
            Surname = default;
            FirstName = default;
            Patronymic = default;
            Birthdate = default;
            Person = default;
            PersonInfo = default;
            ErrorMessage = string.Empty;
        }

        private void ButtonFindPersonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime.TryParse(Birthdate, out var bDate);

                var foundPerson = client.GetPerson(Enp, Snils, Surname, FirstName, Patronymic, bDate);

                if (foundPerson != null)
                {

                    Person = foundPerson.ToString();
                    PersonInfo = client.PersonToPersonInfo(foundPerson).ToString();
                }
                else
                {
                    Person = string.Empty;
                    PersonInfo = string.Empty;
                }

                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            { ErrorMessage = ex.Message; }

        }
    }
}
