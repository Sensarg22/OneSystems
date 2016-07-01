using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Newtonsoft.Json;
using serverServer.Domain;
using serverServer.Exceptions;
using serverServer.Helpers;
using serverServer.ViewModels.Account;
using serverServer.ViewModels.CommonResults;

namespace TestInfrastructure
{
    public static class WebApiExtensions
    {
        public static async Task SignIn(this HttpClient client, LoginViewModel loginViewModel, ClientType clientType)
        {
            var result = await client.PostJson<SignInResultViewModel>($"auth/signIn?type={clientType.ToString().ToLower()}", loginViewModel);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, result.Token);
        }

        public static async Task<CommonResult> SignOut(this HttpClient client, LoginViewModel loginViewModel, ClientType clientType)
        {
            var result = await client.PostJson<CommonResult>($"auth/signOut", loginViewModel);
            return result;
        }

        public static async Task<TRes> PostJson<TRes>(this HttpClient client, string path, object data)
        {
            var model = data != null ? JsonConvert.SerializeObject(data) : string.Empty;
            var stringContent = string.IsNullOrEmpty(model)
                ? new StringContent("")
                : new StringContent(model, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/" + path, stringContent, CancellationToken.None);
            var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync(), Encoding.UTF8);
            var responseString = streamReader.ReadToEnd();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var result = JsonConvert.DeserializeObject<TRes>(responseString);
                return result;
            }
            var error = JsonConvert.DeserializeObject<Error>(responseString);
            throw new Exception(error.ToString());
        }

        public static async Task<TRes> PostFile<TRes>(this HttpClient client, string path, string filePath, string fileName = "test.png")
        {
            var formDataContent = new MultipartFormDataContent();
            var file = File.ReadAllBytes(filePath);

            var fileContent = new ByteArrayContent(file);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

            formDataContent.Add(fileContent, "file", fileName);

            client.DefaultRequestHeaders.Accept.Clear();

            var response =  await client.PostAsync("/api/" + path, formDataContent);
            var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync(), Encoding.UTF8);
            var responseString = streamReader.ReadToEnd();

            client.DefaultRequestHeaders.Accept.Clear();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var result = JsonConvert.DeserializeObject<TRes>(responseString);
                return result;
            }
            var error = JsonConvert.DeserializeObject<Error>(responseString);
            throw new Exception(error.ToString());
        }

        public static async Task<TRes> GetJson<TRes>(this HttpClient client, string path, object parametrs = null)
        {

            var response = await client.GetAsync(UrlHelper.Url("/api/" + path, parametrs), CancellationToken.None);
            var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync(), Encoding.UTF8);
            var responseString = streamReader.ReadToEnd();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var result = JsonConvert.DeserializeObject<TRes>(responseString);
                return result;
            }
            var error = JsonConvert.DeserializeObject<Error>(responseString);
            throw new Exception(error.ToString());
        }


    }
}