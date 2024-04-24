// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VariantServiceDemo
{
    [VariantServiceAlias("RemoteCalculator")]
    public class RemoteCalculator : ICalculator
    {
        private readonly HttpClient _httpClient;

        private class CalculationResult
        {
            [JsonPropertyName("operation")]
            public string Operation { get; set; }

            [JsonPropertyName("expression")]
            public string Expression { get; set; }

            [JsonPropertyName("result")]
            public string Result { get; set; }
        }

        public RemoteCalculator()
        {
            _httpClient = new HttpClient()
            {
                //
                // newton api is a free public api for numerical calculation and symbolic math parsing: https://github.com/aunyks/newton-api
                BaseAddress = new Uri("https://newton.now.sh")
            };
        }

        public async Task<double> AddAsync(double a, double b)
        {
            try
            {
                using HttpResponseMessage response = await _httpClient.GetAsync($"api/v2/simplify/{a}%2B{b}");

                response.EnsureSuccessStatusCode();

                string jsonResponse = await response.Content.ReadAsStringAsync();

                CalculationResult calculationResult = JsonSerializer.Deserialize<CalculationResult>(jsonResponse);

                return double.Parse(calculationResult.Result);
            }
            catch
            {
                await Task.Delay(1000);

                return a + b;
            }
        }
    }
}
