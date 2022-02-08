# Configuration Schemas

This folder contains the schemas for the configuration used by the Microsoft.FeatureManagement library.

# 1.0.0

The [1.0.0 schema](./FeatureManagement.v1.0.0.json) is supported by Microsoft.FeatureManagement version 1.x - 3.x.

* Allows feature flags to be defined.

# 2.0.0

The [2.0.0 schema](./FeatureManagement.v2.0.0.json) is supported by Microsoft.FeatureManagement version 3.x.

* Allows dynamic features to be defined.
* Uses a more explicit path to define feature flags.
  * "FeatureManagement:FeatureFlags:{flagName}" instead of "FeatureManagement:{flagName}".
