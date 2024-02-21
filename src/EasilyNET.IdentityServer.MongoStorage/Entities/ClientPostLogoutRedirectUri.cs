﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace EasilyNET.IdentityServer.MongoStorage.Entities;

/// <summary>
/// ClientPostLogoutRedirectUri
/// </summary>
public class ClientPostLogoutRedirectUri
{
    /// <summary>
    /// PostLogoutRedirectUri
    /// </summary>
    public string PostLogoutRedirectUri { get; set; } = string.Empty;
}