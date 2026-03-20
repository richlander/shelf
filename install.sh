#!/bin/sh
# Install script for shelf.
# Usage: curl --proto '=https' --tlsv1.2 -sSf https://github.com/richlander/shelf/raw/refs/heads/main/install.sh | sh
#
# Installs shelf via dotnet-install if available,
# otherwise downloads a pre-built Native AOT binary from GitHub Releases.
#
# No .NET SDK required.
#
# Environment variables:
#   SHELF_FEED        Override the download base URL
#   SHELF_INSTALL_DIR Override the install directory

set -u

FEED="${SHELF_FEED:-https://github.com/richlander/shelf/releases/download}"
INSTALL_DIR="${SHELF_INSTALL_DIR:-$HOME/.dotnet/bin}"

main() {
    # Prefer dotnet-install if available
    if command -v dotnet-install > /dev/null 2>&1; then
        say "installing via dotnet-install"
        dotnet-install --package Shelf
        return $?
    fi

    downloader --check
    need_cmd uname
    need_cmd tar
    need_cmd mktemp
    need_cmd chmod
    need_cmd mkdir
    need_cmd rm

    get_rid || return 1
    local _rid="$RETVAL"
    assert_nz "$_rid" "rid"

    local _version="0.3.0" # replaced during publishing
    local _url="${FEED}/v${_version}/shelf-${_rid}.tar.gz"

    local _dir
    _dir="$(ensure mktemp -d)"
    local _archive="${_dir}/shelf.tar.gz"

    say "downloading shelf ${_version} (${_rid})"

    ensure mkdir -p "$_dir"
    ensure downloader "$_url" "$_archive" "$_rid"
    ensure tar -xzf "$_archive" -C "$_dir"

    local _bin="${_dir}/shelf"
    ensure chmod u+x "$_bin"

    if [ ! -x "$_bin" ]; then
        err "cannot execute $_bin (may be noexec mount)"
    fi

    # Place binary in install directory
    ensure mkdir -p "$INSTALL_DIR"
    rm -f "$INSTALL_DIR/shelf"
    ensure cp "$_bin" "$INSTALL_DIR/shelf"
    ensure chmod +x "$INSTALL_DIR/shelf"

    say "installed to ${INSTALL_DIR}/shelf"

    # Clean up archive
    rm -rf "$_dir"

    # Check if install dir is on PATH
    case ":$PATH:" in
        *":$INSTALL_DIR:"*) ;;
        *) say "add $INSTALL_DIR to your PATH" ;;
    esac
}

get_rid() {
    local _os _arch
    _os="$(uname -s)"
    _arch="$(uname -m)"

    case "$_os" in
        Linux)
            _os="linux"
            ;;
        Darwin)
            _os="osx"
            ;;
        *)
            err "unsupported OS: $_os"
            ;;
    esac

    case "$_arch" in
        aarch64 | arm64)
            _arch="arm64"
            ;;
        x86_64 | x86-64 | x64 | amd64)
            _arch="x64"
            ;;
        *)
            err "unsupported architecture: $_arch"
            ;;
    esac

    RETVAL="${_os}-${_arch}"
}

say() {
    printf 'shelf: %s\n' "$1" 1>&2
}

err() {
    say "error: $1"
    exit 1
}

need_cmd() {
    if ! command -v "$1" > /dev/null 2>&1; then
        err "need '$1' (command not found)"
    fi
}

assert_nz() {
    if [ -z "$1" ]; then err "assert_nz $2"; fi
}

ensure() {
    if ! "$@"; then err "command failed: $*"; fi
}

downloader() {
    local _dld
    if command -v curl > /dev/null 2>&1; then
        _dld=curl
    elif command -v wget > /dev/null 2>&1; then
        _dld=wget
    else
        _dld='curl or wget'
    fi

    if [ "$1" = --check ]; then
        need_cmd "$_dld"
    elif [ "$_dld" = curl ]; then
        curl --proto '=https' --tlsv1.2 \
            --silent --show-error --fail --location \
            --retry 3 \
            "$1" --output "$2" || {
            err "download failed for platform '$3'; binary may not be available for this platform"
        }
    elif [ "$_dld" = wget ]; then
        wget --https-only --secure-protocol=TLSv1_2 \
            "$1" -O "$2" || {
            err "download failed for platform '$3'; binary may not be available for this platform"
        }
    fi
}

main "$@" || exit 1
