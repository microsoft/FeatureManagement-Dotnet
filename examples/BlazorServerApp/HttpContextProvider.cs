// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace BlazorServerApp
{
    public class HttpContextProvider
    {
        public bool IsAuthenticated { get; set; }

        public string Username { get; set; }
    }
}
