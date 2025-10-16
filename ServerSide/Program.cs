using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.ResponseCompression;
using Newtonsoft.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Syncfusion.Licensing;

namespace WordEditorServices
{
    public class Program
    {
        
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var MyAllowSpecificOrigins = "AllowAllOrigins";

            var configuration = builder.Configuration;
            var env = builder.Environment;

            builder.Services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
            });

            builder.Services.AddMemoryCache();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(MyAllowSpecificOrigins, policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            builder.Services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = System.IO.Compression.CompressionLevel.Optimal;
            });

            builder.Services.AddResponseCompression();

            var app = builder.Build();

            string licenseKey = string.Empty;

            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "SyncfusionLicense.txt")))
            {
                //Assigning Syncfusion LICENSE_KEY from local file.
                licenseKey = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "SyncfusionLicense.txt")).Trim();
            }
            if (string.IsNullOrEmpty(licenseKey))
            {
                // Access LICENSE_KEY from environment variables
                licenseKey = builder.Configuration["SYNCFUSION_LICENSE_KEY"];
                SyncfusionLicenseProvider.RegisterLicense(licenseKey);
            }
            else
            {
                SyncfusionLicenseProvider.RegisterLicense(licenseKey);
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors(MyAllowSpecificOrigins);
            app.UseAuthorization();
            app.UseResponseCompression();

            app.MapControllers();

            app.Run();
        }

    }
}