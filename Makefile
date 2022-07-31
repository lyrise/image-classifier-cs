test:
	dotnet format --verify-no-changes
	dotnet test

build:
	dotnet build

update-sln:
	bash ./eng/update-sln.sh

clean:
	rm -rf ./bin
	rm -rf ./tmp
	rm -rf ./pub

.PHONY: test build
