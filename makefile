cur = ${shell pwd | awk -F'/' '{print $$NF}'}
out = bin/Debug/net10.0
files = ${out}/${cur}.*
version = ${shell grep Version src/main.cs | awk -F'=>' '{print $$2}' | tr -d '\ \";'}
api = ../../api

# Find NuGet package paths from project.nuget.cache
# usage: $(call nuget_path,package-name)
nuget_root = ${shell pwsh -Command "Get-Content obj/project.nuget.cache | ConvertFrom-Json | Select-Object -ExpandProperty expectedPackageFiles | Select-String '${1}' | Select-Object -First 1" 2>/dev/null || \
             grep -i "${1}" obj/project.nuget.cache | tr -d ' ,\"' | head -1}
# Strip the .sha512 filename to get the package dir
nuget_dir = $(dir ${call nuget_root,${1}})

all:
	rm -f *.zip
	rm -f ${cur}.dll ${cur}.pdb ${cur}.deps.json
	dotnet build
	cp ${files} .
	cp ${out}/${cur}.deps.json .
	# Copy NuGet dependency DLLs not already provided by CS2 API
	for dll in ${out}/*.dll; do \
		name=$$(basename "$$dll"); \
		case "$$name" in \
			${cur}.dll|${cur}_api.dll|CounterStrikeSharp.API.dll) ;; \
			*) if [ ! -f "${api}/$$name" ]; then cp "$$dll" .; fi ;; \
		esac; \
	done
	# Copy native SQLite libraries from NuGet cache (Windows + Linux)
	sqlitepkg=$$(grep -i "sqlitepclraw.lib.e_sqlite3" obj/project.nuget.cache | tr -d ' ,"' | head -1); \
	sqlitepkgdir=$$(dirname "$$sqlitepkg"); \
	if [ -n "$$sqlitepkgdir" ] && [ -f "$$sqlitepkgdir/runtimes/win-x64/native/e_sqlite3.dll" ]; then \
		cp "$$sqlitepkgdir/runtimes/win-x64/native/e_sqlite3.dll" .; \
		echo "  copied e_sqlite3.dll (Windows native SQLite)"; \
	fi; \
	if [ -n "$$sqlitepkgdir" ] && [ -f "$$sqlitepkgdir/runtimes/linux-x64/native/libe_sqlite3.so" ]; then \
		cp "$$sqlitepkgdir/runtimes/linux-x64/native/libe_sqlite3.so" .; \
		echo "  copied libe_sqlite3.so (Linux native SQLite)"; \
	fi

.PHONY: release_full
release_full:
	mkdir -p addons/counterstrikesharp/plugins/${cur}
	mkdir -p addons/counterstrikesharp/configs/plugins/${cur}
	mkdir -p addons/counterstrikesharp/shared/${cur}_api
	cp ${cur}.* addons/counterstrikesharp/plugins/${cur}/
	cp ${cur}.deps.json addons/counterstrikesharp/plugins/${cur}/
	# Copy all extra DLLs (non-plugin) into the plugin dir
	for dll in *.dll; do \
		name=$$(basename "$$dll"); \
		case "$$name" in \
			${cur}.dll|${cur}_api.dll) ;; \
			*) cp "$$dll" addons/counterstrikesharp/plugins/${cur}/ ;; \
		esac; \
	done
	# Also copy native SQLite libraries
	if [ -f e_sqlite3.dll ]; then cp e_sqlite3.dll addons/counterstrikesharp/plugins/${cur}/; fi
	if [ -f libe_sqlite3.so ]; then cp libe_sqlite3.so addons/counterstrikesharp/plugins/${cur}/; fi
	cp ../../configs/plugins/${cur}/${cur}.json addons/counterstrikesharp/configs/plugins/${cur}/
	cp ../../shared/${cur}_api/${cur}_api.* addons/counterstrikesharp/shared/${cur}_api/
	zip -r "${cur}_${version}.zip" addons/*
	rm -rf addons