# EAVFramework
![Nuget](https://img.shields.io/nuget/v/EAVFramework)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/EAVFramework)
[![Release](https://github.com/EAVFW/EAVFramework/actions/workflows/release.yml/badge.svg)](https://github.com/EAVFW/EAVFramework/actions/workflows/release.yml)
[![Release](https://github.com/EAVFW/EAVFramework/actions/workflows/release.yml/badge.svg?branch=dev)](https://github.com/EAVFW/EAVFramework/actions/workflows/release.yml)

EAVFrameowrk is a framework for dynamic data modelling from sepcification. The specification is a json configuration file that then in memory or by code generation will generalte model classes for EF Core. 

Allowing you to create dynamic data bases at run time or code time from configuration rathan code. 

We typically refer to run time models as Dynamic Models, and statically generalted code for Static Models. We use this for form generators and such where a dynamic data model is needed for SQL server rather than som document store.


# EAVFramework.SourceGenerator

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



# Validation

Generic validation can be added and uses the `manifest.json` to validate input based on its type, e.g., a string can
have a max- or minLength as described in the `manifest`.

| Validation property | Default Error                        | Number of error args |
| ------------------- | ------------------------------------ | -------------------- |
| **String**          |                                      |                      |
| `minLength`         | Minimum length is {0}                | 1                    |
| `maxLength`         | Maximum length is {0}                | 1                    |
| **Numbers**         |                                      |                      |
| `minimum`           | Much be larger than {0}              | 1                    |
| `exclusiveMinimum`  | Much be larger than or equal to {0}  | 1                    |
| `maximum`           | Much be smaller than {0}             | 1                    |
| `exclusiveMaximum`  | Much be smaller than or equal to {0} | 1                    |

**The error code is the property prefixed with `err-`, e.g., `err-minLength`**.

# Required attributes
Attributes can be required, i.e. required=true in the associated type object in the manifest.

The error code for this is `err-required` and default error is `Is a required field`. No error args is provided.
