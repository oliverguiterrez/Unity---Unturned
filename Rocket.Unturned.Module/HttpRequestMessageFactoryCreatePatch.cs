﻿using System;
using Harmony;
using NuGet.Protocol;
using System.Net.Http;

namespace Rocket.Unturned.Module
{
    [HarmonyPatch(typeof(HttpRequestMessageFactory))]
    [HarmonyPatch(nameof(HttpRequestMessageFactory.Create))]
    [HarmonyPatch(new[] { typeof(HttpMethod), typeof(string), typeof(HttpRequestMessageConfiguration) })]
    public class HttpRequestMessageFactoryCreatePatchString
    {
        [HarmonyPrefix]
        public static void Create(HttpMethod method, ref string requestUri, HttpRequestMessageConfiguration configuration)
        {
#if DEBUG
            System.Console.WriteLine("HttpRequestMessageFactory: Creating from string: " + requestUri);
#endif
            requestUri = requestUri.Replace("https://", "http://");
        }
    }

    [HarmonyPatch(typeof(HttpRequestMessageFactory))]
    [HarmonyPatch(nameof(HttpRequestMessageFactory.Create))]
    [HarmonyPatch(new[] { typeof(HttpMethod), typeof(Uri), typeof(HttpRequestMessageConfiguration) })]
    public class HttpRequestMessageFactoryCreatePatchUri
    {
        [HarmonyPrefix]
        public static void Create(HttpMethod method, ref Uri requestUri, HttpRequestMessageConfiguration configuration)
        {
#if DEBUG
            System.Console.WriteLine("HttpRequestMessageFactory: Creating from URI: " + requestUri);
#endif
            requestUri = new Uri(requestUri.ToString().Replace("https://", "http://"));
        }
    }
}