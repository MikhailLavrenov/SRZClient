using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace SrzClient
{
    /// <summary>
    /// Представляет http клиент для поиска застрахованных в web-портале СРЗ ХК ФОМС.
    /// Перед поиском застрахованных необходимо авторизоваться.
    /// </summary>
    public class WebSrzClient
    {
        Uri baseAddress;
        CookieContainer cookies;
        WebProxy proxy;
        string proxyAddress;
        ushort? proxyPort;

        /// <summary>
        /// Таймаут ожидания одного запроса (мс).
        /// </summary>
        public int Timeout { get; set; } = 60000;
        /// <summary>
        /// Лимит автоматических авторизаций в случае неожиданного разрыва сессии.
        /// </summary>
        public int AutoAuthorizeLimit { get; set; } = 3;
        /// <summary>
        /// Указывает авторизована (true) или нет (false) учетная запись на портале СРЗ/
        /// </summary>
        public bool IsAuthorized { get; private set; }


        /// <summary>
        /// Инициализирует новый экземпляр класса c указанием адреса web-портала, адреса и порта прокси сервера.
        /// </summary>
        /// <param name="webAddress">Адрес web-портала. Если не указан протокол по умочанию принимается http://.</param>
        /// <param name="proxyAddress">Адрес прокси-сервера. Если не используется - null.</param>
        /// <param name="proxyPort">Порт прокси-сервера. Если не используется - null.</param>
        public WebSrzClient(string webAddress, string proxyAddress, ushort? proxyPort)
        {
            if (string.IsNullOrEmpty(webAddress))
                throw new ArgumentNullException("Адрес web-регистра СРЗ не может быть пустым.");

            if (!webAddress.StartsWith("http://") && !webAddress.StartsWith("https://"))
                webAddress = "http://" + webAddress;

            baseAddress = new Uri(webAddress);

            this.proxyAddress = proxyAddress;
            this.proxyPort = proxyPort;

            if (string.IsNullOrEmpty(proxyAddress) || proxyPort == null)
                proxy = null;
            else
            {
                proxy = new WebProxy(proxyAddress, proxyPort.Value);

                if (!TryConnectProxy())
                    throw new ArgumentException("Не удалось подключиться к прокси-серверу. Проверьте правильность адреса и порта. ");
            }

            if (!TryConnectWebsite())
                throw new ArgumentException("Не удалось подключиться к web-регистру СРЗ. Проверьте правильность адреса.");

            cookies = new CookieContainer();
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса c указанием адреса web-портала.
        /// </summary>
        /// <param name="webAddress">Адрес web-портала. Если не указан протокол по умочанию принимается http://.</param>
        public WebSrzClient(string webAddress)
            : this(webAddress, null, null)
        { }


        /// <summary>
        /// Авторизация на web-портале по логину и паролю.
        /// </summary>
        /// <param name="login">Логин.</param>
        /// <param name="password">Пароль.</param>
        public void Authorize(string login, string password)
        {
            if (string.IsNullOrEmpty(login))
                throw new ArgumentException("Логин не может быть пустым.");

            if (password == null)
                password = string.Empty;

            var content = new Dictionary<string, string> {
                { "lg", login },
                { "pw", password}
            };

            try
            {
                var responseText = SendRequest(HttpMethod.Post, @"/data/user.ajax.logon.php", content);

                IsAuthorized = responseText == string.Empty;
            }
            catch (Exception)
            {
                IsAuthorized = false;
                throw;
            }
        }

        /// <summary>
        /// Выход из текущей учетной записи.
        /// </summary>
        public void Logout()
        {
            SendRequest(HttpMethod.Get, @"?show=logoff", null);
            IsAuthorized = false;
        }

        /// <summary>
        /// Поиск застрахованного в web-регистре по одному или нескольким параметрам. 
        /// При поиске по фамилии, имени и/или отчеству можно указывать только начало строк.
        /// </summary>
        /// <param name="enp">Единый номер полиса.</param>
        /// <param name="snils">СНИЛС в формате: "999-999-999 99" где 9 - любое число.</param>
        /// <param name="surname">Фамилия.</param>
        /// <param name="name">Имя.</param>
        /// <param name="patronymic">Отчество.</param>
        /// <param name="birthdate">Дата рождения.</param>
        ///<returns>Экземпляр типа Person.</returns>
        public Person GetPerson(string enp = null, string snils = null, string surname = null, string name = null, string patronymic = null, DateTime? birthdate = null)
        {
            if (!IsAuthorized)
                throw new UnauthorizedAccessException("Для поиска застрахованных необходимо авторизоваться.");

            var content = new Dictionary<string, string> { { "mode", "1" } };

            if (!string.IsNullOrEmpty(enp))
                content.Add("person_enp", enp);

            if (!string.IsNullOrEmpty(snils))
            {
                if (!Regex.IsMatch(snils, @"^\d{3}-\d{3}-\d{3} \d{2}$"))
                    throw new ArgumentException("СНИЛС имеет не верный формат.");

                content.Add("person_snils", snils);
            }

            if (!string.IsNullOrEmpty(surname))
                content.Add("person_surname", surname);

            if (!string.IsNullOrEmpty(name))
                content.Add("person_firname", name);

            if (!string.IsNullOrEmpty(patronymic))
                content.Add("person_secname", patronymic);

            if (birthdate != null)
            {
                content.Add("person_birthday_dm", birthdate.Value.ToString("dd.MM"));
                content.Add("person_birthday_y", birthdate.Value.Year.ToString());
            }

            var responseText = SendRequest(HttpMethod.Post, @"data/reg.person.polis.search.php", content);

            var data = responseText.Split(new string[] { "||" }, StringSplitOptions.None);

            if (data.Length != 47)
            {
                if (responseText.Equals("0||Ошибка авторизации", StringComparison.OrdinalIgnoreCase))
                {
                    IsAuthorized = false;
                    throw new UnauthorizedAccessException("Сессия истекла или произошел иной сбой на стороне web-регистра. Необходимо повторно авторизоваться.");
                }

                if (responseText.Equals("0||В регистре было найдено более одно человека. Пожалуйста, уточните условия поиска", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Найдено более одного человека. Необходимо уточнить критери поиска.");

                if (responseText.Equals("0||Указано недостаточно данных для поиска"))
                    throw new InvalidOperationException("Указано недостаточно данных для поиска.");

                throw new InvalidOperationException("Ответ сервера имеет неверный формат или произошла другая непредвиденная ошибка.");
            }

            var person = new Person()
            {
                PersonId = StringToNullableInt(data[0]),
                PolicyId = StringToNullableInt(data[1]),
                ENP = data[2],
                Surname = data[3],
                Name = data[4],
                Patronymic = data[5],
                Sex = data[6],
                Birthdate = StringToNullableDateTime(data[7]),
                Birthplace = data[8],
                Privilege = data[9],
                Snils = data[10],
                Phone = data[31],
                CitizenshipCode = StringToNullableInt(data[32]),
                DocumentKindCode = StringToNullableInt(data[33]),
                DocumentSer = data[34],
                DocumentNum = data[35],
                DocumentDate = StringToNullableDateTime(data[36]),
                PolicyCompany = data[37],
                PolicyRegistration = data[38],
                Job = data[39],
                PolicyKind = data[40],
                PolicyValidFrom = StringToNullableDateTime(data[41]),
                PolicyModify = StringToNullableDateTime(data[42]),
                PolicyValidTo = StringToNullableDateTime(data[43]),
                PolicyOkato = data[44],
                Status = data[45],
            };

            var regAddress = new InternalAddress()
            {
                FullAddress = data[11],
                PostIndex = StringToNullableInt(data[12]),
                House = data[13],
                Building = data[14],
                Flat = StringToNullableInt(data[15]),
                RegionCode = StringToNullableInt(data[16]),
                DistrictCode = StringToNullableInt(data[17]),
                CityCode = StringToNullableInt(data[18]),
                TownCode = StringToNullableInt(data[19]),
                StreetCode = StringToNullableInt(data[20]),
            };

            var factAddress = new InternalAddress()
            {
                FullAddress = data[21],
                PostIndex = StringToNullableInt(data[22]),
                House = data[23],
                Building = data[24],
                Flat = StringToNullableInt(data[25]),
                RegionCode = StringToNullableInt(data[26]),
                DistrictCode = StringToNullableInt(data[27]),
                CityCode = StringToNullableInt(data[28]),
                TownCode = StringToNullableInt(data[29]),
                StreetCode = StringToNullableInt(data[30]),
            };

            GetPersonPolicies(person);
            person.RegAddress = GetPersonAddress(regAddress);
            person.FactAddress = GetPersonAddress(factAddress);

            return person;
        }

        /// <summary>
        /// Поиск застрахованного в web-регистре по одному или нескольким параметрам. 
        /// При поиске по фамилии, имени и/или отчеству можно указывать только начало строк.
        /// </summary>
        /// <param name="enp">Единый номер полиса.</param>
        /// <param name="snils">СНИЛС в формате: "999-999-999 99" где 9 - любое число.</param>
        /// <param name="surname">Фамилия.</param>
        /// <param name="name">Имя.</param>
        /// <param name="patronymic">Отчество.</param>
        /// <param name="birthdate">Дата рождения.</param>
        ///<returns>Экземпляр типа PersonInfo.</returns>
        public PersonInfo GetPersonInfo(string enp = null, string snils = null, string surname = null, string name = null, string patronymic = null, DateTime? birthdate = null)
            => PersonToPersonInfo(GetPerson(enp, snils, surname, name, patronymic, birthdate));

        /// <summary>
        /// Поиск застрахованного в web-регистре по ЕНП. 
        /// </summary>
        /// <param name="enp">Единый номер полиса.</param>
        ///<returns>Экземпляр типа PersonInfo.</returns>
        public PersonInfo GetPersonInfoByEnp(string enp)
            => GetPersonInfo(enp: enp);

        /// <summary>
        /// Поиск застрахованного в web-регистре по СНИЛС.
        /// </summary>
        /// <param name="snils">СНИЛС в формате: "999-999-999 99" где 9 - любое число.</param>
        ///<returns>Экземпляр типа PersonInfo.</returns>
        public PersonInfo GetPersonInfoBySnils(string snils)
            => GetPersonInfo(snils: snils);

        /// <summary>
        /// Поиск застрахованного в web-регистре по ФИО и дате рождения.
        /// При поиске по фамилии, имени и/или отчеству можно указывать только начало строк.
        /// </summary>
        /// <param name="surname">Фамилия.</param>
        /// <param name="name">Имя.</param>
        /// <param name="patronymic">Отчество.</param>
        /// <param name="birthdate">Дата рождения.</param>
        ///<returns>Экземпляр типа PersonInfo.</returns>
        public PersonInfo GetPersonInfoByFullname(string surname, string name, string patronymic, DateTime? birthdate)
            => GetPersonInfo(surname: surname, name: name, patronymic: patronymic, birthdate: birthdate);

        private void GetPersonPolicies(Person person)
        {
            var responseText = SendRequest(HttpMethod.Get, $@"data/reg.polis.xml.php?person_id={person.PersonId}", null);

            var values = AllSubstringsBetween(responseText, "<cell>", "</cell>");

            if (values.Count % 7 != 0)
                return;

            for (int i = 0; i < values.Count / 7; i++)
            {
                person.Policies.Add(new Policy
                {
                    Organisation = values[7 * i + 0],
                    Kind = values[7 * i + 1],
                    Number = values[7 * i + 2],
                    Issued = StringToNullableDateTime(values[7 * i + 3]),
                    ValidTo = StringToNullableDateTime(values[7 * i + 4]),
                    Closed = StringToNullableDateTime(values[7 * i + 5]),
                    CloseReason = values[7 * i + 6],
                });
            }
        }

        private Address GetPersonAddress(InternalAddress address)
        {
            //в адресе запроса передается 13 значный номер, судя по названию - рандомный, зачем неизвестно, работает и без него, сделал на всякий случай)
            var random = new Random();

            var random13DigitNumber = new StringBuilder(1);

            for (int i = 0; i < 12; i++)
                random13DigitNumber.Append(random.Next(0, 9));

            var relativeAddress = $@"data/reg.address.form.php?height=344&width=588&corpus={address.Building}&field_prefix=r&flat={address.Flat}&house={address.House}&klcity_id={address.CityCode}&klrgn_id={address.RegionCode}&klstreet_id={address.StreetCode}&klsubrgn_id={address.StreetCode}&kltown_id={address.DistrictCode}&name={address.FullAddress}&zip={address.PostIndex}&random={random13DigitNumber}";

            var responseText = SendRequest(HttpMethod.Get, relativeAddress, null);

            int lastIndex = 0;

            address.Region = SubstringBetween(responseText, lastIndex, "klrgn_name", "value=\"", "\"", out lastIndex);
            address.RegionPrefix = SubstringBetween(responseText, lastIndex, "klrgn_socr_name", "value=\"", "\"", out lastIndex);
            address.Town = SubstringBetween(responseText, lastIndex, "klsubrgn_nam", "value=\"", "\"", out lastIndex);
            address.TownPrefix = SubstringBetween(responseText, lastIndex, "klsubrgn_socr_name", "value=\"", "\"", out lastIndex);
            address.City = SubstringBetween(responseText, lastIndex, "klcity_name", "value=\"", "\"", out lastIndex);
            address.CityPrefix = SubstringBetween(responseText, lastIndex, "klcity_socr_name", "value=\"", "\"", out lastIndex);
            address.District = SubstringBetween(responseText, lastIndex, "kltown_name", "value=\"", "\"", out lastIndex);
            address.DistrictPrefix = SubstringBetween(responseText, lastIndex, "kltown_socr_name", "value=\"", "\"", out lastIndex);
            address.Street = SubstringBetween(responseText, lastIndex, "klstreet_id", "selected=\"selected\">", "<", out lastIndex);
            address.StreetPrefix = SubstringBetween(responseText, lastIndex, "klstreet_socr_id", "selected=\"selected\">", "<", out lastIndex);

            return address;
        }

        private string SendRequest(HttpMethod httpMethod, string relativeAddress, IDictionary<string, string> contentParameters)
        {
            if (relativeAddress == null)
                relativeAddress = string.Empty;

            var uri = new Uri(baseAddress, relativeAddress);

            var request = (HttpWebRequest)WebRequest.Create(uri);

            request.Timeout = Timeout;
            request.Method = httpMethod.ToString();
            request.ContentType = "application/x-www-form-urlencoded";
            request.CookieContainer = cookies;

            if (proxy != null)
                request.Proxy = proxy;

            if (httpMethod == HttpMethod.Post && (contentParameters?.Any() ?? false))
            {
                var contentString = new StringBuilder();

                foreach (var item in contentParameters)
                    contentString.Append($"{item.Key}={item.Value}&");

                contentString.Remove(contentString.Length - 1, 1);

                var contentBytes = Encoding.UTF8.GetBytes(contentString.ToString());

                request.ContentLength = contentBytes.Length;

                using (var stream = request.GetRequestStream())
                    stream.Write(contentBytes, 0, contentBytes.Length);
            }

            var asyncResult = request.BeginGetResponse(null, null);

            asyncResult.AsyncWaitHandle.WaitOne(Timeout, true);

            if (!asyncResult.IsCompleted)
            {
                request.Abort();
                throw new TimeoutException($"Отправка завпроса web-регистру не завершилась за отведенное время ({Timeout / 1000} сек.).");
            }

            string result;

            using (var response = (HttpWebResponse)(request.EndGetResponse(asyncResult)))
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream, Encoding.GetEncoding(response.CharacterSet)))
            {
                result = reader.ReadToEnd();
            }

            return result;
        }

        private bool TryConnectProxy()
        {
            using (var client = new TcpClient())
            {
                try
                {
                    client.ReceiveTimeout = Timeout;
                    client.SendTimeout = Timeout;
                    var result = client.BeginConnect(proxyAddress, proxyPort.Value, null, null);
                    result.AsyncWaitHandle.WaitOne(Timeout, true);

                    return client.Connected;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        private bool TryConnectWebsite()
        {
            try
            {
                SendRequest(HttpMethod.Get, null, null);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private int? StringToNullableInt(string text)
        {
            if (string.IsNullOrEmpty(text))
                return default;

            return int.TryParse(text, out int result) ? result : default;
        }

        private DateTime? StringToNullableDateTime(string text)
        {
            if (string.IsNullOrEmpty(text))
                return default;

            return DateTime.TryParse(text, out DateTime result) ? result : default;
        }

        /// <summary>
        /// Преобразует экземпляр типа Person в PersonInfo.
        /// </summary>
        /// <param name="person">Экземпляр типа Person.</param>
        /// <returns>Экземпляр типа PersonInfo.</returns>
        public PersonInfo PersonToPersonInfo(Person person)
        {
            var personInfo = new PersonInfo
            {
                _P_ID = default,
                _VPOLIS = default,
                _SPOLIS = default,
                _NPOLIS = default,
                _ST_OKATO = default,
                _SMO = default,
                _SMO_OGRN = default,
                //здесь я не уверен!!!
                _SMO_OK = person.PolicyOkato?.Split(new string[] { " " }, StringSplitOptions.None).FirstOrDefault(),
                _SMO_NAM = person.PolicyCompany,
                _SOC = default,
                _NOVOR = default,
                _VNOV_D = default,
                _FAM = person.Surname,
                _IM = person.Name,
                _OT = person.Patronymic,
                _W = default,
                _DR = person.Birthdate,
                _DOST = default,
                _TEL = person.Phone,
                _FAM_P = default,
                _IM_P = default,
                _OT_P = default,
                _W_P = default,
                _DR_P = default,
                _DOST_P = default,
                _MR = person.Birthplace,
                _MPR = person.FactAddress?.FullAddress,
                _DOCTYPE = person.DocumentKindCode?.ToString(),
                _DOCSER = person.DocumentSer,
                _DOCNUM = person.DocumentNum,
                _DOCDATE = person.DocumentDate,
                _SNILS = person.Snils,
                _WORK = person.Job,
                _OKATOG = default,
                _OKATOP = default,
                _COMENTP = default,
                _NHISTORY = default,
                _KLADR = default,
                _DOCORG = default,
                _ADRESS = default
            };

            if (person.RegAddress != null)
            {

                personInfo._ADRESS = new PersonAdress
                {
                    _P_ID = default,
                    _RGN = default,
                    //здесь я не уверен!!!
                    _KRAY = string.IsNullOrEmpty(person.RegAddress.Region) ? default : $"{person.RegAddress.RegionPrefix} {person.RegAddress.Region}",
                    _RN = string.IsNullOrEmpty(person.RegAddress.District) ? default : $"{person.RegAddress.DistrictPrefix} {person.RegAddress.District}",
                    //здесь я не уверен!!!
                    _TOWN = string.IsNullOrEmpty(person.RegAddress.Town) ? default : $"{person.RegAddress.TownPrefix} {person.RegAddress.Town}",
                    _CITY = string.IsNullOrEmpty(person.RegAddress.City) ? default : $"{person.RegAddress.CityPrefix} {person.RegAddress.City}",
                    _STREET = string.IsNullOrEmpty(person.RegAddress.Street) ? default : $"{person.RegAddress.StreetPrefix} {person.RegAddress.Street}",
                    _DOM = person.RegAddress.House.ToString(),
                    _KOR = person.RegAddress.Building.ToString(),
                    _STR = default,
                    _KV = person.RegAddress.Flat.ToString(),
                };
            }

            switch (person.PolicyKind.ToUpper())
            {
                case "ПОЛИС ОМС СТАРОГО ОБРАЗЦА":
                    personInfo._VPOLIS = 1;
                    break;
                case "ВРЕМЕННОЕ СВИДЕТЕЛЬСТВО":
                    personInfo._VPOLIS = 2;
                    break;
                case "БУМАЖНЫЙ ПОЛИС ОМС ЕДИНОГО ОБРАЗЦА":
                case "ЭЛЕКТРОННЫЙ ПОЛИС ОМС ЕДИНОГО ОБРАЗЦА":
                    personInfo._VPOLIS = 3;
                    break;
                default:
                    personInfo._VPOLIS = null;
                    break;
            }

            var policyParts = person.Policies?.Last().Number?.Split(new char[] { ' ' });

            switch (policyParts?.Length ?? 0)
            {
                case 1:
                    personInfo._NPOLIS = policyParts[0];
                    break;
                case 2:
                    personInfo._SPOLIS = policyParts[0];
                    personInfo._NPOLIS = policyParts[1];
                    break;
            }

            switch (person.Sex.ToUpper())
            {
                case "МУЖСКОЙ":
                    personInfo._W = 1;
                    break;
                case "ЖЕНСКИЙ":
                    personInfo._W = 2;
                    break;
                default:
                    personInfo._W = null;
                    break;
            }

            return personInfo;
        }

        /// <summary>
        /// Находит все подстроки заключенные между левой и правой строками.
        /// </summary>
        /// <param name="text">Текст по которому осуществляется поиск.</param>
        /// <param name="leftBound">Левая строка после которой начинается искомая подстрока.</param>
        /// <param name="rightBound">Правая строка перед которой находится искомая подстрока.</param>
        /// <returns></returns>
        private static List<string> AllSubstringsBetween(string text, string leftBound, string rightBound)
        {
            int index = 0;

            var substrings = new List<string>();

            while (true)
            {
                var index1 = text.IndexOf(leftBound, index);

                if (index1 == -1)
                    break;

                index1 += leftBound.Length;

                var index2 = text.IndexOf(rightBound, index1);

                if (index2 == -1)
                    break;

                index = index2 + rightBound.Length;

                var length = index2 - index1;

                if (length > 0)
                    substrings.Add(text.Substring(index1, length));
                else
                    substrings.Add(string.Empty);
            }

            return substrings;
        }

        /// <summary>
        /// Находит в тексте ключевую строку, затем возвращает подстроку между левой и правой строками.
        /// Обычно 1ая строка уникальная в тексте, остальные 2 встречаются часто и используются для более точного выделения искомого текста.
        /// </summary>
        /// <param name="text">Текст по которому осуществляется поиск.</param>
        /// <param name="startIndex">Начальный индекс для поиска. Используется для оптимизации.</param>
        /// <param name="keyStr">Ключевая строка после которой начинается поиск.</param>
        /// <param name="leftBound">Левая строка после которой начинается искомая подстрока.</param>
        /// <param name="rightBound">Правая строка перед которой находится искомая подстрока.</param>
        /// <param name="lastIndex">Возвращает индекс правой подстроки. Используется для оптимизации последующих запросов.</param>
        /// <returns></returns>
        private static string SubstringBetween(string text, int startIndex, string keyStr, string leftBound, string rightBound, out int lastIndex)
        {
            lastIndex = 0;

            int index = startIndex;

            if (!string.IsNullOrEmpty(keyStr))
            {
                index = text.IndexOf(keyStr, index);

                if (index == -1)
                    return null;
            }

            var index1 = text.IndexOf(leftBound, index);

            if (index1 == -1)
                return null;

            index1 += leftBound.Length;

            var index2 = text.IndexOf(rightBound, index1);

            if (index2 == -1)
                return null;

            lastIndex = index2 + rightBound.Length;

            var length = index2 - index1;

            return length > 0 ? text.Substring(index1, length) : string.Empty;
        }


        private enum HttpMethod
        {
            Post,
            Get,
        }


        private class InternalAddress : Address
        {
            public int? RegionCode { get; set; }
            public int? DistrictCode { get; set; }
            public int? CityCode { get; set; }
            public int? TownCode { get; set; }
            public int? StreetCode { get; set; }
        }
    }
}
