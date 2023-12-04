# Blazor Server App

This sample shows the recommended way to use Feature Management in Blazor Server App.

## Quickstart

To run this sample, follow these steps:

1. Set the project as the startup project.
2. Run the project.
3. Enter a username and click the login button.

### About the App

This app demonstrates how to use Feature Management in Blazor apps. It will display some enhanced contents for vip users.
It follows [this document](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/server/threat-mitigation?view=aspnetcore-7.0#ihttpcontextaccessorhttpcontext-in-razor-components) to mitigate security threats while using HttpContext.
This app uses [cookie authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-6.0) for user login.

### Vip User

Username "admin" is a vip user.
Any username which ends with "@vip.com" (e.g. "test@vip.com") will be designated as a vip user.
Vip users can see some enhanced contents including larger welcome text and golden username in the profile page.