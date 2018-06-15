﻿/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/openiddict/openiddict-core for more information concerning
 * the license and the contributors participating to this project.
 */

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AspNet.Security.OpenIdConnect.Primitives;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace OpenIddict.Server.Tests
{
    public class OpenIddictServerBuilderTests
    {
        [Fact]
        public void AddEventHandler_HandlerIsAttached()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            var handler = new OpenIddictServerEventHandler<ApplyAuthorizationResponse>(
                (notification, cancellationToken) => Task.FromResult(0));

            // Act
            builder.AddEventHandler(handler);

            // Assert
            Assert.Contains(services, service =>
                service.ServiceType == typeof(IOpenIddictServerEventHandler<ApplyAuthorizationResponse>) &&
                service.ImplementationInstance == handler);
        }

        [Fact]
        public void AddEventHandler_ThrowsAnExceptionForInvalidHandlerType()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act and assert
            var exception = Assert.Throws<ArgumentException>(delegate
            {
                return builder.AddEventHandler<ApplyAuthorizationResponse>(typeof(object));
            });

            Assert.Equal("type", exception.ParamName);
            Assert.StartsWith("The specified type is invalid.", exception.Message);
        }

        [Fact]
        public void AddEventHandler_HandlerIsRegistered()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.AddEventHandler<ApplyAuthorizationResponse, CustomHandler>();

            // Assert
            Assert.Contains(services, service =>
                service.ServiceType == typeof(IOpenIddictServerEventHandler<ApplyAuthorizationResponse>) &&
                service.ImplementationType == typeof(CustomHandler));
        }

        [Fact]
        public void Configure_OptionsAreCorrectlyAmended()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.Configure(configuration => configuration.Description.DisplayName = "OpenIddict");

            var options = GetOptions(services);

            // Assert
            Assert.Equal("OpenIddict", options.Description.DisplayName);
        }

        [Fact]
        public void AddEphemeralSigningKey_SigningKeyIsCorrectlyAdded()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.AddEphemeralSigningKey();

            var options = GetOptions(services);

            // Assert
            Assert.Equal(1, options.SigningCredentials.Count);
        }

        [Theory]
        [InlineData(SecurityAlgorithms.RsaSha256Signature)]
        [InlineData(SecurityAlgorithms.RsaSha384Signature)]
        [InlineData(SecurityAlgorithms.RsaSha512Signature)]
#if SUPPORTS_ECDSA
        [InlineData(SecurityAlgorithms.EcdsaSha256Signature)]
        [InlineData(SecurityAlgorithms.EcdsaSha384Signature)]
        [InlineData(SecurityAlgorithms.EcdsaSha512Signature)]
#endif
        public void AddEphemeralSigningKey_SigningCredentialsUseSpecifiedAlgorithm(string algorithm)
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.AddEphemeralSigningKey(algorithm);

            var options = GetOptions(services);
            var credentials = options.SigningCredentials[0];

            // Assert
            Assert.Equal(algorithm, credentials.Algorithm);
        }

        [Theory]
        [InlineData(SecurityAlgorithms.HmacSha256Signature)]
        [InlineData(SecurityAlgorithms.RsaSha256Signature)]
#if SUPPORTS_ECDSA
        [InlineData(SecurityAlgorithms.EcdsaSha256Signature)]
        [InlineData(SecurityAlgorithms.EcdsaSha384Signature)]
        [InlineData(SecurityAlgorithms.EcdsaSha512Signature)]
#endif
        public void AddSigningKey_SigningKeyIsCorrectlyAdded(string algorithm)
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            var factory = Mock.Of<CryptoProviderFactory>(mock =>
                mock.IsSupportedAlgorithm(algorithm, It.IsAny<SecurityKey>()));

            var key = Mock.Of<SecurityKey>(mock => mock.CryptoProviderFactory == factory);

            // Act
            builder.AddSigningKey(key);

            var options = GetOptions(services);

            // Assert
            Assert.Same(key, options.SigningCredentials[0].Key);
        }

        [Fact]
        public void AddSigningCertificate_SigningKeyIsCorrectlyAdded()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.AddSigningCertificate(
                assembly: typeof(OpenIddictServerBuilderTests).GetTypeInfo().Assembly,
                resource: "OpenIddict.Server.Tests.Certificate.pfx",
                password: "OpenIddict");

            var options = GetOptions(services);

            // Assert
            Assert.IsType(typeof(X509SecurityKey), options.SigningCredentials[0].Key);
        }

        [Fact]
        public void AllowAuthorizationCodeFlow_CodeFlowIsAddedToGrantTypes()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.AllowAuthorizationCodeFlow();

            var options = GetOptions(services);

            // Assert
            Assert.Contains(OpenIdConnectConstants.GrantTypes.AuthorizationCode, options.GrantTypes);
        }

        [Fact]
        public void AllowClientCredentialsFlow_ClientCredentialsFlowIsAddedToGrantTypes()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.AllowClientCredentialsFlow();

            var options = GetOptions(services);

            // Assert
            Assert.Contains(OpenIdConnectConstants.GrantTypes.ClientCredentials, options.GrantTypes);
        }

        [Fact]
        public void AllowCustomFlow_CustomFlowIsAddedToGrantTypes()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.AllowCustomFlow("urn:ietf:params:oauth:grant-type:custom_grant");

            var options = GetOptions(services);

            // Assert
            Assert.Contains("urn:ietf:params:oauth:grant-type:custom_grant", options.GrantTypes);
        }

        [Fact]
        public void AllowImplicitFlow_ImplicitFlowIsAddedToGrantTypes()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.AllowImplicitFlow();

            var options = GetOptions(services);

            // Assert
            Assert.Contains(OpenIdConnectConstants.GrantTypes.Implicit, options.GrantTypes);
        }

        [Fact]
        public void AllowPasswordFlow_PasswordFlowIsAddedToGrantTypes()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.AllowPasswordFlow();

            var options = GetOptions(services);

            // Assert
            Assert.Contains(OpenIdConnectConstants.GrantTypes.Password, options.GrantTypes);
        }

        [Fact]
        public void AllowRefreshTokenFlow_RefreshTokenFlowIsAddedToGrantTypes()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.AllowRefreshTokenFlow();

            var options = GetOptions(services);

            // Assert
            Assert.Contains(OpenIdConnectConstants.GrantTypes.RefreshToken, options.GrantTypes);
        }

        [Fact]
        public void DisableAuthorizationStorage_AuthorizationStorageIsDisabled()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.DisableAuthorizationStorage();

            var options = GetOptions(services);

            // Assert
            Assert.True(options.DisableAuthorizationStorage);
        }

        [Fact]
        public void DisableConfigurationEndpoint_ConfigurationEndpointIsDisabled()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.DisableConfigurationEndpoint();

            var options = GetOptions(services);

            // Assert
            Assert.Equal(PathString.Empty, options.ConfigurationEndpointPath);
        }

        [Fact]
        public void DisableCryptographyEndpoint_CryptographyEndpointIsDisabled()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.DisableCryptographyEndpoint();

            var options = GetOptions(services);

            // Assert
            Assert.Equal(PathString.Empty, options.CryptographyEndpointPath);
        }

        [Fact]
        public void DisableSlidingExpiration_SlidingExpirationIsDisabled()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.DisableSlidingExpiration();

            var options = GetOptions(services);

            // Assert
            Assert.False(options.UseSlidingExpiration);
        }

        [Fact]
        public void DisableTokenStorage_TokenStorageIsDisabled()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.DisableTokenStorage();

            var options = GetOptions(services);

            // Assert
            Assert.True(options.DisableTokenStorage);
        }

        [Fact]
        public void EnableAuthorizationEndpoint_AuthorizationEndpointIsEnabled()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.EnableAuthorizationEndpoint("/endpoint-path");

            var options = GetOptions(services);

            // Assert
            Assert.Equal("/endpoint-path", options.AuthorizationEndpointPath);
        }

        [Fact]
        public void EnableIntrospectionEndpoint_IntrospectionEndpointIsEnabled()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.EnableIntrospectionEndpoint("/endpoint-path");

            var options = GetOptions(services);

            // Assert
            Assert.Equal("/endpoint-path", options.IntrospectionEndpointPath);
        }

        [Fact]
        public void EnableLogoutEndpoint_LogoutEndpointIsEnabled()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.EnableLogoutEndpoint("/endpoint-path");

            var options = GetOptions(services);

            // Assert
            Assert.Equal("/endpoint-path", options.LogoutEndpointPath);
        }

        [Fact]
        public void EnableRequestCaching_RequestCachingIsEnabled()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.EnableRequestCaching();

            var options = GetOptions(services);

            // Assert
            Assert.True(options.EnableRequestCaching);
        }

        [Fact]
        public void EnableRevocationEndpoint_RevocationEndpointIsEnabled()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.EnableRevocationEndpoint("/endpoint-path");

            var options = GetOptions(services);

            // Assert
            Assert.Equal("/endpoint-path", options.RevocationEndpointPath);
        }

        [Fact]
        public void DisableScopeValidation_ScopeValidationIsDisabled()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.DisableScopeValidation();

            var options = GetOptions(services);

            // Assert
            Assert.True(options.DisableScopeValidation);
        }

        [Fact]
        public void EnableTokenEndpoint_TokenEndpointIsEnabled()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.EnableTokenEndpoint("/endpoint-path");

            var options = GetOptions(services);

            // Assert
            Assert.Equal("/endpoint-path", options.TokenEndpointPath);
        }

        [Fact]
        public void EnableUserinfoEndpoint_UserinfoEndpointIsEnabled()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.EnableUserinfoEndpoint("/endpoint-path");

            var options = GetOptions(services);

            // Assert
            Assert.Equal("/endpoint-path", options.UserinfoEndpointPath);
        }

        [Fact]
        public void AcceptAnonymousClients_ClientIdentificationIsOptional()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.AcceptAnonymousClients();

            var options = GetOptions(services);

            // Assert
            Assert.True(options.AcceptAnonymousClients);
        }

        [Fact]
        public void SetAccessTokenLifetime_DefaultAccessTokenLifetimeIsReplaced()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.SetAccessTokenLifetime(TimeSpan.FromMinutes(42));

            var options = GetOptions(services);

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(42), options.AccessTokenLifetime);
        }

        [Fact]
        public void SetAuthorizationCodeLifetime_DefaultAuthorizationCodeLifetimeIsReplaced()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.SetAuthorizationCodeLifetime(TimeSpan.FromMinutes(42));

            var options = GetOptions(services);

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(42), options.AuthorizationCodeLifetime);
        }

        [Fact]
        public void SetIdentityTokenLifetime_DefaultIdentityTokenLifetimeIsReplaced()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.SetIdentityTokenLifetime(TimeSpan.FromMinutes(42));

            var options = GetOptions(services);

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(42), options.IdentityTokenLifetime);
        }

        [Fact]
        public void SetRefreshTokenLifetime_DefaultRefreshTokenLifetimeIsReplaced()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.SetRefreshTokenLifetime(TimeSpan.FromMinutes(42));

            var options = GetOptions(services);

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(42), options.RefreshTokenLifetime);
        }

        [Fact]
        public void SetIssuer_AddressIsReplaced()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.SetIssuer(new Uri("http://www.fabrikam.com/"));

            var options = GetOptions(services);

            // Assert
            Assert.Equal(new Uri("http://www.fabrikam.com/"), options.Issuer);
        }

        [Fact]
        public void RegisterClaims_ClaimsAreAdded()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.RegisterClaims("custom_claim_1", "custom_claim_2");

            var options = GetOptions(services);

            // Assert
            Assert.Contains("custom_claim_1", options.Claims);
            Assert.Contains("custom_claim_2", options.Claims);
        }

        [Fact]
        public void RegisterScopes_ScopesAreAdded()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.RegisterScopes("custom_scope_1", "custom_scope_2");

            var options = GetOptions(services);

            // Assert
            Assert.Contains("custom_scope_1", options.Scopes);
            Assert.Contains("custom_scope_2", options.Scopes);
        }

        [Fact]
        public void UseDataProtectionProvider_DefaultProviderIsReplaced()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.UseDataProtectionProvider(new EphemeralDataProtectionProvider());

            var options = GetOptions(services);

            // Assert
            Assert.IsType(typeof(EphemeralDataProtectionProvider), options.DataProtectionProvider);
        }

        [Fact]
        public void UseJsonWebTokens_AccessTokenHandlerIsCorrectlySet()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.UseJsonWebTokens();

            var options = GetOptions(services);

            // Assert
            Assert.IsType<JwtSecurityTokenHandler>(options.AccessTokenHandler);
        }

        [Fact]
        public void UseReferenceTokens_ReferenceTokensAreEnabled()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            var builder = CreateBuilder(services);

            // Act
            builder.UseReferenceTokens();

            var options = GetOptions(services);

            // Assert
            Assert.True(options.UseReferenceTokens);
        }

        private static IServiceCollection CreateServices()
            => new ServiceCollection().AddOptions();

        private static OpenIddictServerBuilder CreateBuilder(IServiceCollection services)
            => new OpenIddictServerBuilder(services);

        private static OpenIddictServerOptions GetOptions(IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<OpenIddictServerOptions>>();
            return options.Value;
        }

        public class CustomHandler : OpenIddictServerEventHandler<ApplyAuthorizationResponse>
        {
            public CustomHandler(Func<ApplyAuthorizationResponse, CancellationToken, Task> handler) : base(handler)
            {
            }
        }
    }
}
