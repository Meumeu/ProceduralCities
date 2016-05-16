KSPDIR  ?= ${HOME}/.local/share/Steam/SteamApps/common/Kerbal\ Space\ Program
MANAGED := ${KSPDIR}/KSP_Data/Managed/

SOURCEFILES := $(wildcard ProceduralCities/*.cs) $(wildcard Common/*.cs) $(wildcard ProceduralCities/WorldObjects/*.cs)
RESGEN2 := resgen2
GMCS    ?= mcs
GIT     := git
ZIP     := zip

VERSION_MAJOR := 0
VERSION_MINOR := 1
VERSION_PATCH := 0

VERSION := ${VERSION_MAJOR}.${VERSION_MINOR}.${VERSION_PATCH}

ifeq ($(debug),1)
	DEBUG = -debug -define:DEBUG
endif

all: build/ProceduralCities.dll

info:
	@echo "== ProceduralCities Build Information =="
	@echo "  resgen2: ${RESGEN2}"
	@echo "  gmcs:    ${GMCS}"
	@echo "  git:     ${GIT}"
	@echo "  zip:     ${ZIP}"
	@echo "  KSP Data: ${KSPDIR}"
	@echo "================================"

build/%.dll: ${SOURCEFILES}
	mkdir -p build
	${GMCS} -t:library -lib:${MANAGED} \
		-r:Assembly-CSharp,Assembly-CSharp-firstpass,UnityEngine,KSPUtil \
		${DEBUG} \
		-out:$@ \
		${SOURCEFILES}
#		-resource:ProceduralCities/Resources/AlphaUnlitVertexColored.txt,ProceduralCities.Resources.AlphaUnlitVertexColored.txt



package: build/ProceduralCities.dll
	mkdir -p package/ProceduralCities
	cp -r Assets package/ProceduralCities/
	cp $< package/ProceduralCities/

%.zip:
	cd package && ${ZIP} -9 -r ../$@ ProceduralCities

zip: package ProceduralCities-${VERSION}.zip

release: zip
	git commit -m "release v${VERSION}" Makefile
	git tag v${VERSION}

clean:
	@echo "Cleaning up build and package directories..."
	rm -rf build/ package/

install: package
	cp -r package/ProceduralCities ${KSPDIR}/GameData/

uninstall: info
	rm -rf ${KSPDIR}/GameData/ProceduralCities


.PHONY : all info build package zip clean install uninstall
