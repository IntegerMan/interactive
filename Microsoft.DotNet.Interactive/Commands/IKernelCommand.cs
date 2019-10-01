﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Commands
{
    public interface IKernelCommand
    {
        Task InvokeAsync(KernelInvocationContext context);
        IDictionary<string, object> Properties { get; }
    }
}