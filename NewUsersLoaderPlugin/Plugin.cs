using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PhoneApp.Domain;
using PhoneApp.Domain.Attributes;
using PhoneApp.Domain.DTO;
using PhoneApp.Domain.Interfaces;
using System.Net;

namespace EmployeesNewLoader
{
    [Author(Name = "Kirill Rykhlov")]
    public class Plugin : IPluggable
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private const string ApiUrl = "https://dummyjson.com/users";

        public IEnumerable<DataTransferObject> Run(IEnumerable<DataTransferObject> args)
        {
            Console.WriteLine("Starting Users Loader Plugin");
            logger.Info("Starting Users Loader Plugin");

            var employeesList = args.Cast<EmployeesDTO>().ToList();
            logger.Info($"Loaded {employeesList.Count} existing users.");

            var newUsers = GetUsersFromApi().Result;
            if (newUsers != null)
            {
                employeesList.AddRange(newUsers);
                logger.Info($"Added {newUsers.Count} new users from API.");
            }
            else
            {
                logger.Error("Failed to load users from API.");
            }

            return employeesList.Cast<DataTransferObject>();
        }

        private async Task<List<EmployeesDTO>> GetUsersFromApi()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            using (var client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(ApiUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonData = await response.Content.ReadAsStringAsync();
                        var apiResponse = JsonConvert.DeserializeObject<ApiUsersResponse>(jsonData);

                        // Преобразуем данные API в формат EmployeesDTO
                        var employees = apiResponse.Users.Select(user => new EmployeesDTO
                        {
                            Name = $"{user.FirstName} {user.LastName}",
                            Phone = user.Phone
                        }).ToList();

                        return employees;
                    }
                    else
                    {
                        logger.Error($"Error fetching users from API. Status code: {response.StatusCode}");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"Exception while fetching users from API: {ex.Message}");
                    return null;
                }
            }
        }
    }

    public class ApiUsersResponse
    {
        public List<ApiUser> Users { get; set; }
    }

    public class ApiUser
    {
        [JsonProperty("firstName")]
        public string FirstName { get; set; }

        [JsonProperty("lastName")]
        public string LastName { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }
    }
}
