﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Http.Parsing;
using Microsoft.DotNet.Interactive.Http.Parsing.Parsing;
using Microsoft.DotNet.Interactive.Http.Tests.Utility;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Http.Tests;

public partial class ParserTests
{
    public class Variables
    {
        [Fact]
        public void expression_is_parsed_correctly()
        {
            var result = Parse(
                """
                GET https://{{host}}/api/{{version}}comments/1 HTTP/1.1
                Authorization: {{token}}
                """);

            var requestNode = result.SyntaxTree.RootNode.ChildNodes
                                    .Should().ContainSingle<HttpRequestNode>().Which;

            requestNode.UrlNode.DescendantNodesAndTokens().OfType<HttpExpressionNode>().Select(e => e.Text)
                       .Should().BeEquivalentSequenceTo(new[] { "host", "version" });

            requestNode.HeadersNode.DescendantNodesAndTokens().OfType<HttpExpressionNode>()
                       .Should().ContainSingle().Which.Text.Should().Be("token");
        }

        [Fact]
        public void variable_in_document_is_parsed_correctly()
        {
            var result = Parse(
                """
                @host = https://httpbin.org/                

                POST {{host}}/anything HTTP/1.1
                content-type: application/json

                {
                    "name": "sample1",
                }

                
                """
                );

            var variableNode = result.SyntaxTree.RootNode.ChildNodes
                                      .Should().ContainSingle<HttpVariableDeclarationAndAssignmentNode>().Which;

            variableNode.DeclarationNode.VariableName.Should().Be("host");
            variableNode.ValueNode.Text.Should().Be("https://httpbin.org/");
        }

        [Fact]
        public void multiple_variables_in_document_are_parsed_correctly()
        {
            var result = Parse(
                """
                @host = https://httpbin.org/   
                @version = HTTP/1.1

                POST {{host}}/anything {{version}}
                content-type: application/json

                {
                    "name": "sample1",
                }

                
                """
                );

            var variableNodes = result.SyntaxTree.RootNode.DescendantNodesAndTokens()
                .OfType<HttpVariableDeclarationAndAssignmentNode>();

            variableNodes.Select(v => v.DeclarationNode.VariableName).Should().BeEquivalentSequenceTo(new[] { "host", "version" });
            variableNodes.Select(e => e.ValueNode.Text).Should().BeEquivalentSequenceTo(new[] { "https://httpbin.org/", "HTTP/1.1" });
        }

        [Fact]
        public void variable_using_another_variable_is_parsed_correctly()
        {
            var result = Parse(
                """             
                @hostname = httpbin.org
                @host = https://{{hostname}}/                             

                POST {{host}}/anything HTTP/1.1
                content-type: application/json

                {
                    "name": "sample1",
                }

                
                """
                );

            var variableNodes = result.SyntaxTree.RootNode.DescendantNodesAndTokens()
                                       .OfType<HttpVariableDeclarationAndAssignmentNode>();

            variableNodes.Select(v => v.DeclarationNode.VariableName).Should()
                .BeEquivalentSequenceTo(new[] { "hostname", "host" });

            variableNodes.Select(e => e.ValueNode.Text).Should().BeEquivalentSequenceTo(new[] { "httpbin.org", "https://{{hostname}}/" });
        }

        [Fact]
        public void no_variable_name_produces_diagnostic()
        {
            var result = Parse(
                """             
                @ = httpbin.org
                @host = https://{{hostname}}/                             

                POST {{host}}/anything HTTP/1.1
                content-type: application/json

                {
                    "name": "sample1",
                }

                
                """
                );

            var variableNodes = result.SyntaxTree.RootNode.DescendantNodesAndTokens()
                                       .OfType<HttpVariableDeclarationNode>();

            variableNodes.First().GetDiagnostics().Single().GetMessage().Should().Be("Variable name expected.");
        }

        [Fact]
        public void multiple_variables_with_comments_are_parsed_correctly()
        {
            var result = Parse(
                """             
                @hostname = httpbin.org
                # variable using another variable
                @host = https://{{hostname}}/
                # variable using "dynamic variables"                              

                POST {{host}}/anything HTTP/1.1
                content-type: application/json

                {
                    "name": "sample1",
                }

                
                """
                );

            var rootNode = result.SyntaxTree.RootNode;

            var declarationNodes = result.SyntaxTree.RootNode.DescendantNodesAndTokens()
                                       .OfType<HttpVariableDeclarationNode>();

            var variableDeclarationNode = declarationNodes.Select(v => v.VariableName).Should()
                .BeEquivalentSequenceTo(new[] { "hostname", "host" });
        }

        [Fact]
        public void declared_variables_can_be_used_for_binding()
        {
            var result = Parse(
                """
                @hostname=httpbin.org
                """);

            var variables = result.SyntaxTree.RootNode.GetDeclaredVariables();
            variables.Should().ContainSingle().Which.Value.Value.Should().Be("httpbin.org");
        }

        [Fact]
        public void declared_variables_using_another_variable_can_be_resolved()
        {
            var result = Parse(
                """
                @hostname=httpbin.org
                @host=https://{{hostname}}
                """);

            var variables = result.SyntaxTree.RootNode.GetDeclaredVariables();
            variables.Should().Contain(n => n.Key == "host").Which.Value.Should().BeOfType<DeclaredVariable>().Which.Value.Should().Be("https://httpbin.org");

        }
    }
}