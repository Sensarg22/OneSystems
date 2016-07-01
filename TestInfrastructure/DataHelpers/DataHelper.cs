using System;
using Server.ViewModels.Account;

namespace TestInfrastructure
{
    public class DataHelper
    {
        public static RegisterViewModel GenerateUser()
        {
            return GenerateUser("+8");
        }

        public static RegisterViewModel GenerateUser(string phoneCountryCode)
        {
            var phoneNumber = new Random().Next(111, 999).ToString() + new Random().Next(111, 999) + new Random().Next(11, 99) +
                              new Random().Next(11, 99);

            var fullPhoneNumber = phoneCountryCode + phoneNumber;

            var data = new RegisterViewModel
            {
                PhoneNumber = fullPhoneNumber,
                Email = $"registrTest{fullPhoneNumber.Replace("+","").Trim()}@server.com",
                Password = "testtest",
                ConfirmPassword = "testtest",
                Sandbox = true
            };

            return data;
        }

        public static RegisterViewModel GenerateSequentialUser(int seqq)
        {
            return GenerateSequentialUser("+9", seqq);
        }

        public static RegisterViewModel GenerateSequentialUser(string phoneCountryCode, int seqq)
        {
            var email = GenerateEmail(seqq);

            var phoneNumber = GenerateFilledPhoneNumber(seqq);

            var data = new RegisterViewModel
            {
                PhoneNumber = phoneCountryCode + phoneNumber,
                Email = email,
                Password = "testtest",
                ConfirmPassword = "testtest",
                Sandbox = true
            };

            return data;
        }

        private static String GenerateFilledPhoneNumber(int seq)
        {
            return "999555" + seq.ToString().PadLeft(4, '0');
        }

        private static String GenerateEmail(int seq)
        {
            //Console.WriteLine(MyString.PadRight(20, '-'));
            return $"testsetng0{seq}@servertest.com";
        }

        
    }
}