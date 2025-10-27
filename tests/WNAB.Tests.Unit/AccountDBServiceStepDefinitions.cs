using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Reqnroll;
using Shouldly;
using WNAB.Data;
using WNAB.Services;

namespace WNAB.Tests.Unit;

public partial class StepDefinitions
{
    private WnabContext _db = default!;
    private AccountDBService _svc = default!;
    private Exception? _caught;
    private Account? _createdAccount;

    private static WnabContext CreateInMemoryDb(string name)
    {
        var options = new DbContextOptionsBuilder<WnabContext>()
        .UseInMemoryDatabase(name)
        .EnableSensitiveDataLogging()
        .Options;
        return new WnabContext(options);
    }

    [Given("an empty in-memory database")]
    public void GivenEmptyInMemoryDatabase()
    {
        _db = CreateInMemoryDb(Guid.NewGuid().ToString("N"));
        _svc = new AccountDBService(_db);
    }

    [Given("a user exists with Id1 and email \"(.*)\"")]
    public async Task GivenUserExistsWithId1AndEmail(string email)
    {
        var user = new User { Id = 1, Email = email, FirstName = "U", LastName = "T", IsActive = true };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        context["User"] = user;
    }

    [Given("a pending category change exists")]
    public void GivenPendingCategoryChangeExists()
    {
        var user = context.Get<User>("User");
        _db.Categories.Add(new Category { Id = 10, UserId = user.Id, Name = "Food", IsIncome = false, IsActive = true });
        // do not SaveChanges -> leaves pending change in ChangeTracker
    }

    [When("I create an account named \"(.*)\"")]
    public async Task WhenICreateAnAccountNamed(string name)
    {
        var user = context.Get<User>("User");
        _createdAccount = await _svc.CreateAccountAsync(user, name);
    }

    [When("I attempt to create an account named \"(.*)\"")]
    public async Task WhenIAttemptToCreateAnAccountNamed(string name)
    {
        try
        {
            await WhenICreateAnAccountNamed(name);
        }
        catch (Exception ex)
        {
            _caught = ex;
        }
    }

    [When("I attempt to create an account with a null user")]
    public async Task WhenIAttemptToCreateWithNullUser()
    {
        try
        {
            await _svc.CreateAccountAsync(null!, "X");
        }
        catch (Exception ex)
        {
            _caught = ex;
        }
    }

    [When("I attempt to create an account with a blank name")]
    public async Task WhenIAttemptToCreateWithBlankName()
    {
        var user = new User { Id = 1 };
        try
        {
            await _svc.CreateAccountAsync(user, " ");
        }
        catch (Exception ex)
        {
            _caught = ex;
        }
    }

    [Then("exactly1 account should exist for user1")]
    public async Task ThenExactlyOneAccountShouldExistForUser1()
    {
        var all = await _db.Accounts.Where(a => a.UserId == 1).ToListAsync();
        all.Count.ShouldBe(1);
    }

    [Then("the created account should have name \"(.*)\" and a generated Id")]
    public void ThenCreatedAccountShouldHaveNameAndGeneratedId(string expectedName)
    {
        _createdAccount.ShouldNotBeNull();
        _createdAccount!.Id.ShouldBeGreaterThan(0);
        _createdAccount!.UserId.ShouldBe(1);
        _createdAccount!.AccountName.ShouldBe(expectedName);
    }

    [Then("the operation should fail with (.*)")]
    public void ThenOperationShouldFailWith(string exceptionName)
    {
        _caught.ShouldNotBeNull();
        _caught!.GetType().Name.ShouldBe(exceptionName);
    }
}
