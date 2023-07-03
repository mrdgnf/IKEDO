using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IKEDO
{
    public partial class App : Application
    {
    }
    public enum DocumentType
    {
        Inbox, 
        Sent,
        NoType
    }
    public enum Platform
    {
        Demo,
        Development
    }    
    public class Document
    {
        public string? Id;
        public string? Name;
        public Sender? Sender;
        public List<Recipients>? Recipients;
        public DateTime? DateSent;
    }
    public class Sender
    {
        public Employee? Employee;
        public JobTitle? JobTitle;
    }    
    public class Employee
    {
        public string? FirstName;
        public string? LastName;
        public string? MiddleName;
    }
    public class Person : Employee
    {
        public string? Email;
    }
    public class Recipients : Employee
    {
        public string? JobTitle;
    }
    public class JobTitle
    {
        public string? Name;
    }
    public class TableDocument
    {
        public bool CheckBox { get; set; }
        public string Name { get; set; }
        public List<PersonInfo> PersonsInfo { get; set; }
        public string DateSent { get; set; }
        public TableDocument(bool checkBox, string name, List<PersonInfo> personInfo, string dateSent)
        {
            CheckBox = checkBox;
            Name = name;
            PersonsInfo = personInfo;
            DateSent = dateSent;
        }
    }
    public class PersonInfo
    {
        public string FullName { get; set; }
        public string JobTitle { get; set; }
        public PersonInfo(string fullName, string jobTitle)
        {
            FullName = fullName;
            JobTitle = jobTitle;
        }
    }
    public class Setting
    {
        public Platform Platform { get; set; }
        public string Token { get; set; }
        public Setting(Platform platform, string token)
        {
            Platform = platform;
            Token = token;
        }
    }
    public class DownloadInfo
    {
        public string? FileName { get; set; }
        public string? DocumentID { get; set; }
        public DownloadInfo(string fileName, string documentID)
        {
            FileName = fileName;
            DocumentID = documentID;
        }
    }
    public class APIRequests
    {      
        public static async Task<T?> GetRequest<T>(Setting setting, string? relativePath)
        {
            const string DEV_URL = "https://api-gw-kedo.cloud.astral-dev.ru/api/v3";
            const string DEMO_URL = "https://api-gw.kedo-demo.cloud.astral-dev.ru/api/v3";

            HttpClient httpClient = new();

            string key = "Bearer " + setting.Token;

            string path = "";

            switch (setting.Platform)
            {
                case Platform.Demo:
                    path = DEMO_URL + relativePath;
                    break;
                case Platform.Development:
                    path = DEV_URL + relativePath;
                    break;
                default:
                    break;
            }

            using HttpRequestMessage request = new(HttpMethod.Get, path);

            httpClient.DefaultRequestHeaders.Add("accept", "application/json");

            httpClient.DefaultRequestHeaders.Add("kedo-gateway-token-type", "IntegrationApi");

            httpClient.DefaultRequestHeaders.Add("Authorization", key);

            using HttpResponseMessage response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            string content = await response.Content.ReadAsStringAsync();

            T? data = JsonConvert.DeserializeObject<T>(content);

            return data;           
        }
        private static async Task<string?> DownloadRequest(Setting setting, string? relativePath)
        {
            const string DEV_URL = "https://api-gw-kedo.cloud.astral-dev.ru/api/v3";
            const string DEMO_URL = "https://api-gw.kedo-demo.cloud.astral-dev.ru/api/v3";

            HttpClient httpClient = new();

            string key = "Bearer " + setting.Token;

            string url = "";

            switch (setting.Platform)
            {
                case Platform.Demo:
                    url = DEMO_URL + relativePath;
                    break;
                case Platform.Development:
                    url = DEV_URL + relativePath;
                    break;
                default:
                    break;
            }

            httpClient.DefaultRequestHeaders.Add("accept", "application/json");

            httpClient.DefaultRequestHeaders.Add("kedo-gateway-token-type", "IntegrationApi");

            httpClient.DefaultRequestHeaders.Add("Authorization", key);

            var data = new { DocumentRouteMemberIds = Array.Empty<string>() };

            var json = JsonConvert.SerializeObject(data);

            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await httpClient.PostAsync(url,content);

            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            string rawFiles = await response.Content.ReadAsStringAsync();

            return rawFiles;
        }
        public static async Task<List<Document>?> GetDocuments(Setting setting, DocumentType type)
        {
            string? relativePath = null;

            switch (type)
            {
                case DocumentType.Inbox:
                    relativePath = "/docstorage/Documents/inbox";
                    break;
                case DocumentType.Sent:
                    relativePath = "/docstorage/Documents/sent";
                    break;
                default:
                    break;
            }

            var documents = await GetRequest<List<Document>?>(setting, relativePath);

            return documents;
        }
        public static async Task<Person?> PersonalInfo(Setting setting)
        {
            const string RELATIVE_PATH = "/staff/Employees/PersonalInfo";

            var personalInfo = await GetRequest<Person?>(setting, RELATIVE_PATH);

            return personalInfo;
        }
        public static async Task<string?> DownloadRawDocument(Setting setting, string id)
        {
            string relativePath = $"/docstorage/Documents/{id}/download/signed-files/{DateTimeOffset.Now.Offset:hh\\:mm\\:ss}";

            var rawFile = await DownloadRequest(setting, relativePath);

            return rawFile;
        }
    }
}
