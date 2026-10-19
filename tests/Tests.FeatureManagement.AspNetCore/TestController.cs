// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;

namespace Tests.FeatureManagement.AspNetCore
{
    [Route("")]
    public class TestController : Controller
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok();
        }

        [Route("/gateAll")]
        [HttpGet]
        [FeatureGate(Features.ConditionalFeature, Features.ConditionalFeature2)]
        public IActionResult GateAll()
        {
            return Ok();
        }

        [Route("/gateAny")]
        [HttpGet]
        [FeatureGate(requirementType: RequirementType.Any, Features.ConditionalFeature, Features.ConditionalFeature2)]
        public IActionResult GateAny()
        {
            return Ok();
        }

        [Route("/gateAllNegate")]
        [HttpGet]
        [FeatureGate(negate: true, Features.ConditionalFeature, Features.ConditionalFeature2)]
        public IActionResult GateAllNegate()
        {
            return Ok();
        }

        [Route("/gateAnyNegate")]
        [HttpGet]
        [FeatureGate(requirementType: RequirementType.Any, negate: true, Features.ConditionalFeature, Features.ConditionalFeature2)]
        public IActionResult GateAnyNegate()
        {
            return Ok();
        }
    }
}
