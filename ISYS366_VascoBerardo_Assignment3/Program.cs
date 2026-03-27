using ISYS366_VascoBerardo_Assignment3.Data;
using ISYS366_VascoBerardo_Assignment3.Models;
using ISYS366_VascoBerardo_Assignment3.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddDbContext<ISYS366_VascoBerardo_Assignment3Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ISYS366_VascoBerardo_Assignment3Context") ?? throw new InvalidOperationException("Connection string 'ISYS366_VascoBerardo_Assignment3Context' not found.")));

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ISYS366_VascoBerardo_Assignment3Context>();

builder.Services.AddValidation();

builder.Services.AddAuthorization(options =>
{
    // in our authorization options we add a policy
    // that requires the user to have the admin role
    options.AddPolicy("AdminPolicy", policy =>
    {
        policy.RequireRole("Admin");
    });
});

// add this section to configure options for our razor pages
builder.Services.AddRazorPages(options =>
{
    // secure anything in the Pages/Items folder 
    // by assigning it the admin policy
    // which we created above 
    // saying it requires a user to have the admin role
    options.Conventions.AuthorizeFolder("/Movies", "AdminPolicy");
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    SeedData.Initialize(services);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

using (var scope = app.Services.CreateScope())
{
    var roleManager =
        scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // check if we already have an admin role
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        // if not make the admin role
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    // now we are going to make a default admin user
    var userManager =
        scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    string email = "admin@mostuff.com";
    // DANGER! PASSWORD MUST BE:
    // 6+ chars
    // at least one non alphanumerc character
    // at least one digit ('0'-'9')
    // at least one uppercase ('A'-'Z')
    string password = "Password123!";

    // see if we have already created the user
    // if not create them and give them the admin role
    if (await userManager.FindByEmailAsync(email) == null)
    {
        var user = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, "Admin");
    }
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
    .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    await AdminHelper.SeedAdminAsync(scope.ServiceProvider);
}

app.Run();
