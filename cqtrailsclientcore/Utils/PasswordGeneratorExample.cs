using System;
using cqtrailsclientcore.Utils;

namespace cqtrailsclientcore.Utils
{
    public class PasswordGeneratorExample
    {
        public static void Run()
        {
            Console.WriteLine("Password Generator Example\n");
            
            // Generate a password with default parameters (length 10, with special chars)
            string defaultPassword = PasswordGenerator.GenerateRandomPassword();
            Console.WriteLine($"Default password (length 10): {defaultPassword}");
            
            // Generate a password with custom length
            string customLengthPassword = PasswordGenerator.GenerateRandomPassword(16);
            Console.WriteLine($"Custom length password (length 16): {customLengthPassword}");
            
            // Generate a password without special characters
            string noSpecialCharsPassword = PasswordGenerator.GenerateRandomPassword(12, false);
            Console.WriteLine($"No special chars password (length 12): {noSpecialCharsPassword}");
            
            // Generate multiple passwords to demonstrate randomness
            Console.WriteLine("\nGenerating multiple passwords to demonstrate randomness:");
            for (int i = 0; i < 5; i++)
            {
                string randomPassword = PasswordGenerator.GenerateRandomPassword();
                Console.WriteLine($"  Password {i+1}: {randomPassword}");
            }
        }
    }
} 