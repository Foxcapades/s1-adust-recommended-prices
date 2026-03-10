PROJECT := RecommendedPrice
VERSION := $(shell grep 'MelonInfo' $(PROJECT)/Mod.cs | cut -d'"' -f4 | tr -d '\n')
CONFIGURATION := IL2Cpp
LOWER_CONFIG := $(shell echo $(CONFIGURATION) | tr A-Z a-z)
BUILD_TARGET := $(PROJECT).dll
OUTPUT_TARGET := $(PROJECT).$(CONFIGURATION).dll

ifeq ($(CONFIGURATION),IL2Cpp)
	NETPATH := net6.0
else
	NETPATH := netstandard2.1
endif

.PHONY: default
default:
	@echo "NO"

.PHONY: build
build: target/$(OUTPUT_TARGET)

.PHONY: package
package: target/rp-$(LOWER_CONFIG)-v$(VERSION).zip

.PHONY: release
release: package-il2cpp package-mono

.PHONY: build-mono
build-mono:
	@$(MAKE) CONFIGURATION=Mono build

.PHONY: package-mono
package-mono:
	@$(MAKE) CONFIGURATION=Mono package

.PHONY: build-mono
build-il2cpp:
	@$(MAKE) CONFIGURATION=IL2Cpp build

.PHONY: package-mono
package-il2cpp:
	@$(MAKE) CONFIGURATION=IL2Cpp package

bin/$(CONFIGURATION)/$(NETPATH)/$(BUILD_TARGET): $(PROJECT)/Mod.cs
	@dotnet build -c $(CONFIGURATION)

target/$(OUTPUT_TARGET): bin/$(CONFIGURATION)/$(NETPATH)/$(BUILD_TARGET)
	@mkdir -p target
	@cp bin/$(CONFIGURATION)/$(NETPATH)/$(BUILD_TARGET) target/$(OUTPUT_TARGET)

target/rp-$(LOWER_CONFIG)-v$(VERSION).zip: target/$(OUTPUT_TARGET)
	@cd target && zip -9 $(@F) $(OUTPUT_TARGET)
