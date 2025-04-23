using Xunit;
using cqtrailsclientcore.Utils;
using System.Linq;

namespace cqtrailsclientcore.Utils;

public class PasswordGeneratorTests
{
    [Fact]
    public void GenerateRandomPassword_DefaultParameters_ReturnsExpectedLength()
    {
        // Act
        string password = PasswordGenerator.GenerateRandomPassword();
        
        // Assert
        Assert.Equal(10, password.Length);
    }
    
    [Fact]
    public void GenerateRandomPassword_CustomLength_ReturnsCorrectLength()
    {
        // Arrange
        int expectedLength = 12;
        
        // Act
        string password = PasswordGenerator.GenerateRandomPassword(expectedLength);
        
        // Assert
        Assert.Equal(expectedLength, password.Length);
    }
    
    [Fact]
    public void GenerateRandomPassword_LengthTooSmall_ReturnsMinimumLength()
    {
        // Act
        string password = PasswordGenerator.GenerateRandomPassword(4);
        
        // Assert
        Assert.Equal(8, password.Length); // Should enforce minimum length of 8
    }
    
    [Fact]
    public void GenerateRandomPassword_WithSpecialChars_ContainsAllCharacterTypes()
    {
        // Act
        string password = PasswordGenerator.GenerateRandomPassword(12, true);
        
        // Assert
        Assert.True(password.Any(char.IsLower), "Password should contain at least one lowercase letter");
        Assert.True(password.Any(char.IsUpper), "Password should contain at least one uppercase letter");
        Assert.True(password.Any(char.IsDigit), "Password should contain at least one digit");
        Assert.True(password.Any(c => !char.IsLetterOrDigit(c)), "Password should contain at least one special character");
    }
    
    [Fact]
    public void GenerateRandomPassword_WithoutSpecialChars_DoesNotContainSpecialChars()
    {
        // Act
        string password = PasswordGenerator.GenerateRandomPassword(12, false);
        
        // Assert
        Assert.True(password.Any(char.IsLower), "Password should contain at least one lowercase letter");
        Assert.True(password.Any(char.IsUpper), "Password should contain at least one uppercase letter");
        Assert.True(password.Any(char.IsDigit), "Password should contain at least one digit");
        Assert.False(password.Any(c => !char.IsLetterOrDigit(c)), "Password should not contain special characters");
    }
    
    [Fact]
    public void GenerateRandomPassword_CalledTwice_ReturnsDifferentPasswords()
    {
        // Act
        string password1 = PasswordGenerator.GenerateRandomPassword();
        string password2 = PasswordGenerator.GenerateRandomPassword();
        
        // Assert
        Assert.NotEqual(password1, password2);
    }
} 