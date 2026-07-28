using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using QRemember.Tests.TestHelpers;
using QRemember.Web.Pages.Shared.Events;

namespace QRemember.Tests.Validation;

// These tests bypass the handler methods entirely and run the real
// System.ComponentModel.DataAnnotations pipeline (Validator.TryValidateObject)
// against the bound page-model properties, proving the [Required]/[EmailAddress]/
// [MinLength]/[Compare]/[StringLength] attributes themselves catch bad input —
// as opposed to the handler tests, which simulate an invalid ModelState directly.
public class PageModelValidationTests
{
    private static IList<ValidationResult> Validate(object instance)
    {
        var context = new ValidationContext(instance);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
        return results;
    }

    private static bool HasErrorFor(IList<ValidationResult> results, string memberName) =>
        results.Any(r => r.MemberNames.Contains(memberName));

    // ---------- LoginModel ----------

    private static LoginModel ValidLoginModel() => new(
        IdentityMockFactory.MockSignInManager(IdentityMockFactory.MockUserManager().Object).Object,
        IdentityMockFactory.MockUserManager().Object)
    {
        Email = "user@example.com",
        Password = "password123"
    };

    [Fact]
    public void LoginModel_ValidInput_HasNoValidationErrors()
    {
        Assert.Empty(Validate(ValidLoginModel()));
    }

    [Fact]
    public void LoginModel_MissingEmail_FailsRequired()
    {
        var model = ValidLoginModel();
        model.Email = "";

        Assert.True(HasErrorFor(Validate(model), nameof(LoginModel.Email)));
    }

    [Fact]
    public void LoginModel_MalformedEmail_FailsEmailAddressValidation()
    {
        var model = ValidLoginModel();
        model.Email = "not-an-email";

        Assert.True(HasErrorFor(Validate(model), nameof(LoginModel.Email)));
    }

    [Fact]
    public void LoginModel_MissingPassword_FailsRequired()
    {
        var model = ValidLoginModel();
        model.Password = "";

        Assert.True(HasErrorFor(Validate(model), nameof(LoginModel.Password)));
    }

    // ---------- RegisterModel ----------

    private static RegisterModel ValidRegisterModel() => new(IdentityMockFactory.MockUserManager().Object)
    {
        Name = "Jamie Doe",
        Email = "jamie@example.com",
        Password = "Password123!",
        ConfirmPassword = "Password123!"
    };

    [Fact]
    public void RegisterModel_ValidInput_HasNoValidationErrors()
    {
        Assert.Empty(Validate(ValidRegisterModel()));
    }

    [Fact]
    public void RegisterModel_MissingName_FailsRequired()
    {
        var model = ValidRegisterModel();
        model.Name = "";

        Assert.True(HasErrorFor(Validate(model), nameof(RegisterModel.Name)));
    }

    [Fact]
    public void RegisterModel_MalformedEmail_FailsEmailAddressValidation()
    {
        var model = ValidRegisterModel();
        model.Email = "not-an-email";

        Assert.True(HasErrorFor(Validate(model), nameof(RegisterModel.Email)));
    }

    [Fact]
    public void RegisterModel_ShortPassword_FailsMinLength()
    {
        var model = ValidRegisterModel();
        model.Password = "short1";
        model.ConfirmPassword = "short1";

        Assert.True(HasErrorFor(Validate(model), nameof(RegisterModel.Password)));
    }

    [Fact]
    public void RegisterModel_MismatchedConfirmPassword_FailsCompare()
    {
        var model = ValidRegisterModel();
        model.ConfirmPassword = "differentPassword1";

        Assert.True(HasErrorFor(Validate(model), nameof(RegisterModel.ConfirmPassword)));
    }

    // ---------- ResetPasswordModel ----------

    private static ResetPasswordModel ValidResetPasswordModel() => new(IdentityMockFactory.MockUserManager().Object)
    {
        Email = "jamie@example.com",
        Code = "123456",
        Password = "NewPassword123!",
        ConfirmPassword = "NewPassword123!"
    };

    [Fact]
    public void ResetPasswordModel_ValidInput_HasNoValidationErrors()
    {
        Assert.Empty(Validate(ValidResetPasswordModel()));
    }

    [Fact]
    public void ResetPasswordModel_MalformedEmail_FailsEmailAddressValidation()
    {
        var model = ValidResetPasswordModel();
        model.Email = "not-an-email";

        Assert.True(HasErrorFor(Validate(model), nameof(ResetPasswordModel.Email)));
    }

    [Theory]
    [InlineData("12345")]   // too short
    [InlineData("1234567")] // too long
    public void ResetPasswordModel_CodeNotSixDigitsLong_FailsStringLength(string code)
    {
        var model = ValidResetPasswordModel();
        model.Code = code;

        Assert.True(HasErrorFor(Validate(model), nameof(ResetPasswordModel.Code)));
    }

    [Fact]
    public void ResetPasswordModel_ShortPassword_FailsMinLength()
    {
        var model = ValidResetPasswordModel();
        model.Password = "short1";
        model.ConfirmPassword = "short1";

        Assert.True(HasErrorFor(Validate(model), nameof(ResetPasswordModel.Password)));
    }

    [Fact]
    public void ResetPasswordModel_MismatchedConfirmPassword_FailsCompare()
    {
        var model = ValidResetPasswordModel();
        model.ConfirmPassword = "somethingElse1";

        Assert.True(HasErrorFor(Validate(model), nameof(ResetPasswordModel.ConfirmPassword)));
    }

    // ---------- ForgotPasswordModel ----------

    private static ForgotPasswordModel ValidForgotPasswordModel() => new(
        IdentityMockFactory.MockUserManager().Object,
        new Mock<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>().Object)
    {
        Email = "jamie@example.com"
    };

    [Fact]
    public void ForgotPasswordModel_ValidInput_HasNoValidationErrors()
    {
        Assert.Empty(Validate(ValidForgotPasswordModel()));
    }

    [Fact]
    public void ForgotPasswordModel_MissingEmail_FailsRequired()
    {
        var model = ValidForgotPasswordModel();
        model.Email = "";

        Assert.True(HasErrorFor(Validate(model), nameof(ForgotPasswordModel.Email)));
    }

    [Fact]
    public void ForgotPasswordModel_MalformedEmail_FailsEmailAddressValidation()
    {
        var model = ValidForgotPasswordModel();
        model.Email = "not-an-email";

        Assert.True(HasErrorFor(Validate(model), nameof(ForgotPasswordModel.Email)));
    }

    // ---------- PhotoUploadModel.UploadInput ----------

    private static Mock<IFormFile> MockPhotoFile() => new();

    private static PhotoUploadModel.UploadInput ValidUploadInput() => new()
    {
        UploaderName = "Alex",
        Caption = "A great memory",
        PhotoFile = MockPhotoFile().Object
    };

    [Fact]
    public void UploadInput_ValidInput_HasNoValidationErrors()
    {
        Assert.Empty(Validate(ValidUploadInput()));
    }

    [Fact]
    public void UploadInput_MissingUploaderName_FailsRequired()
    {
        var model = ValidUploadInput();
        model.UploaderName = "";

        Assert.True(HasErrorFor(Validate(model), nameof(PhotoUploadModel.UploadInput.UploaderName)));
    }

    [Fact]
    public void UploadInput_UploaderNameTooLong_FailsMaxLength()
    {
        var model = ValidUploadInput();
        model.UploaderName = new string('a', 101);

        Assert.True(HasErrorFor(Validate(model), nameof(PhotoUploadModel.UploadInput.UploaderName)));
    }

    [Fact]
    public void UploadInput_CaptionTooLong_FailsMaxLength()
    {
        var model = ValidUploadInput();
        model.Caption = new string('a', 501);

        Assert.True(HasErrorFor(Validate(model), nameof(PhotoUploadModel.UploadInput.Caption)));
    }

    [Fact]
    public void UploadInput_MissingPhotoFile_FailsRequired()
    {
        var model = ValidUploadInput();
        model.PhotoFile = null;

        Assert.True(HasErrorFor(Validate(model), nameof(PhotoUploadModel.UploadInput.PhotoFile)));
    }

    // ---------- CreateEventModel ----------

    private static CreateEventModel ValidCreateEventModel() => new(
        InMemoryDbContextFactory.Create(),
        IdentityMockFactory.MockUserManager().Object,
        new Mock<Microsoft.AspNetCore.Hosting.Server.IServer>().Object,
        new Mock<IWebHostEnvironment>().Object)
    {
        Name = "Birthday Bash",
        Description = "Come celebrate with us",
        EventDate = new DateTime(2026, 12, 25)
    };

    [Fact]
    public void CreateEventModel_ValidInput_HasNoValidationErrors()
    {
        Assert.Empty(Validate(ValidCreateEventModel()));
    }

    [Fact]
    public void CreateEventModel_MissingName_FailsRequired()
    {
        var model = ValidCreateEventModel();
        model.Name = "";

        Assert.True(HasErrorFor(Validate(model), nameof(CreateEventModel.Name)));
    }

    [Fact]
    public void CreateEventModel_NameTooLong_FailsMaxLength()
    {
        var model = ValidCreateEventModel();
        model.Name = new string('a', 101);

        Assert.True(HasErrorFor(Validate(model), nameof(CreateEventModel.Name)));
    }

    [Fact]
    public void CreateEventModel_DescriptionTooLong_FailsMaxLength()
    {
        var model = ValidCreateEventModel();
        model.Description = new string('a', 1001);

        Assert.True(HasErrorFor(Validate(model), nameof(CreateEventModel.Description)));
    }

    [Fact]
    public void CreateEventModel_MissingEventDate_FailsRequired()
    {
        var model = ValidCreateEventModel();
        model.EventDate = null;

        Assert.True(HasErrorFor(Validate(model), nameof(CreateEventModel.EventDate)));
    }
}
