.PHONY: default
default:
	@echo "NO"

.PHONY:
build:
	@dotnet build
	@mkdir -p target
	@cp bin/IL2Cpp/net6.0/RecommendedPrice.dll target/RecommendedPrice.IL2Cpp.dll
