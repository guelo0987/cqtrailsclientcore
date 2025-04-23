using System;
using System.Linq;
using System.Security.Cryptography;

namespace cqtrailsclientcore.Utils;

public static class PasswordGenerator
{
    private static readonly char[] LowerCaseLetters = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
    private static readonly char[] UpperCaseLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
    private static readonly char[] Digits = "0123456789".ToCharArray();
    private static readonly char[] SpecialChars = "!@#$%^&*()-_=+[]{}|;:,.<>?".ToCharArray();
    
    public static string GenerateRandomPassword(int length = 10, bool includeSpecialChars = true)
    {
        if (length < 8)
            length = 8; // Enforce minimum length for security
            
        // Define character sets based on parameters
        var allChars = LowerCaseLetters
            .Concat(UpperCaseLetters)
            .Concat(Digits);
            
        if (includeSpecialChars)
            allChars = allChars.Concat(SpecialChars);
            
        char[] allCharsArray = allChars.ToArray();
        
        // Create a buffer to hold random bytes
        var password = new char[length];
        
        // Ensure we have at least one of each required type
        password[0] = GetRandomElement(LowerCaseLetters);
        password[1] = GetRandomElement(UpperCaseLetters);
        password[2] = GetRandomElement(Digits);
        
        if (includeSpecialChars && length > 3)
            password[3] = GetRandomElement(SpecialChars);
            
        // Fill the rest with random characters
        for (int i = includeSpecialChars ? 4 : 3; i < length; i++)
        {
            password[i] = GetRandomElement(allCharsArray);
        }
        
        // Shuffle the password to avoid predictable positioning
        Shuffle(password);
        
        return new string(password);
    }
    
    private static char GetRandomElement(char[] array)
    {
        return array[RandomNumberGenerator.GetInt32(0, array.Length)];
    }
    
    private static void Shuffle(char[] array)
    {
        int n = array.Length;
        while (n > 1)
        {
            n--;
            int k = RandomNumberGenerator.GetInt32(0, n + 1);
            (array[k], array[n]) = (array[n], array[k]); // Swap using tuple
        }
    }
} 