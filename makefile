VERSION := $(shell grep 'MelonInfo' RecommendedPrice/Mod.cs | cut -d'"' -f4 | tr -d '\n')

CONFIGURATION := IL2Cpp
LOWER_CONFIG := $(shell echo $(CONFIGURATION) | tr A-Z a-z)

BUILD_DLL  := RecommendedPrice.dll
OUTPUT_DLL := RecommendedPrice.$(CONFIGURATION).dll

TARGET_DIRECTORY := target
INSTALL_DIRECTORY := ${INSTALL_DIRECTORY}

ifeq ($(CONFIGURATION),IL2Cpp)
	NET_PATH := net6.0
else
	NET_PATH := netstandard2.1
endif

BUILD_TARGET := bin/$(CONFIGURATION)/$(NET_PATH)/$(BUILD_DLL)
OUTPUT_TARGET := $(TARGET_DIRECTORY)/$(OUTPUT_DLL)
RELEASE_ZIP := $(TARGET_DIRECTORY)/rp-$(LOWER_CONFIG)-v$(VERSION).zip

.PHONY: default
default:
	@echo "NO"

.PHONY: build
build: $(OUTPUT_TARGET)

.PHONY: package
package: $(RELEASE_ZIP)

.PHONY: install
install: build
	@if [ ! -z "$(INSTALL_DIRECTORY)" ]; then \
		cp $(OUTPUT_TARGET) "$(INSTALL_DIRECTORY)"; \
	fi

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

$(BUILD_TARGET): RecommendedPrice/Mod.cs
	@dotnet build -c $(CONFIGURATION)

$(OUTPUT_TARGET): $(BUILD_TARGET)
	@mkdir -p $(TARGET_DIRECTORY)
	@cp $(BUILD_TARGET) $(OUTPUT_TARGET)

$(RELEASE_ZIP): $(OUTPUT_TARGET)
	@cd $(TARGET_DIRECTORY) && zip -9 $(@F) $(OUTPUT_DLL)
