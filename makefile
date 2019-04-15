# Executable name
EXE := alto

# Directory names
SRC := src
BIN := bin
OBJ := obj
MAC := mac
WIN := win

# Build configuration
CONFIG := Release

# ------------------------------------------------------------------------------

# Build options
BUILD_OPT += -v quiet
BUILD_OPT += -c $(CONFIG)
MAC_BUILD_OPT += -r osx-x64
WIN_BUILD_OPT += -r win-x64

# Build targets
MAC_EXE := $(BIN)/$(EXE)
WIN_EXE := $(BIN)/$(EXE).exe

# ------------------------------------------------------------------------------

# Recursive wildcard utility
rwildcard = $(wildcard $1$2) $(foreach d,$(wildcard $1*),$(call rwildcard,$d/,$2))

# Source files
SOURCES := $(call rwildcard,$(SRC)/,*.cs)

# Build tools
DOTNET := dotnet
STRIP := strip
RM := rm
RMDIR := rm -r

# Operating system detection
ifneq ($(OS),Windows_NT)
OS := $(shell uname -s)
endif

# ------------------------------------------------------------------------------

.PHONY: all mac win clean

# ------------------------------------------------------------------------------

ifeq ($(OS),Windows_NT)
all: win
endif

ifeq ($(OS),Darwin)
all: mac
endif

# ------------------------------------------------------------------------------

# Mac build
mac: $(MAC_EXE)

$(MAC_EXE): $(SOURCES)
	@$(DOTNET) publish $(BUILD_OPT) $(MAC_BUILD_OPT) -o $(dir $@)
	@$(RMDIR) $(BIN)/$(CONFIG)
	@$(RM) $(dir $@)/*.json
	@$(RM) $(dir $@)/*.pdb
	@$(STRIP) $@

# ------------------------------------------------------------------------------

# Win build
win: $(WIN_EXE)

$(WIN_EXE): $(SOURCES)
	@$(DOTNET) publish $(BUILD_OPT) $(WIN_BUILD_OPT) -o $(dir $@)

# ------------------------------------------------------------------------------

# Clean output
clean:
	-@$(RMDIR) $(BIN) $(OBJ)
