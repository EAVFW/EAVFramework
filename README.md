
# DotNetDevOps Extensions for EAV Modelling

This library adds supprot for dynamic data modelling and a EAV framework on top of EF Core


# DotNetDevOps.Extensions.EAVFramework.SourceGenerator

The generator works by creating the same in memory dll that the runtime framework does. From this in memory dll it generates the sourcecode needed. 

Currently Methods cant be genrerated, so its only DTO properties.


# Authentication

Inspired by Easy Auth, EAVFramework provides a implementation that are similar to Easy Auth defined here: https://docs.microsoft.com/en-us/azure/app-service/configure-authentication-customize-sign-in-out


Start the login flow to a given provider

GET https://host/account/login (servers a login page)
GET https://host/.auth/login/<provider>
REDIRECT to provider
GET https://host/.auth/login/<provider>/callback
REDIRECT to /account/login/callback
REDIRECT to application


error: access_denied
error_subcode: cancel
state: redir=
error_description: AADSTS65004: User declined to consent to access the app.