# Changelog

All notable changes to this project will be documented in this file.

---

# [2.1.2] - 2025-12-16
### Added
- [Request Builder] WithSection(string section)
- [Request Builder] WithoutSection()
- [Exceptions] HttpServerErrorException for 500/429
- [Exceptions] HttpErrorException for other
### Modified
- [Request Builder] WithUrl payload changed from Uri to string
- [Request Builder] WithUrl can set only the part after section url now
### Removed
- [Logging] url parameter, header and body for security

## [2.1.1] - 2025-12-13
### Added
- all meta files

## [2.1.0] - 2025-12-13
### Removed
- all meta files

## [2.0.9] - 2025-12-06
### Modified
- [package.json] add doc links

## [2.0.8] 2025-11-30
### Modified
- [Request Builder] warning on parameter key or value is empty or null

## [2.0.7] 2025-11-09
### Fixed
- [IRequestHandle] fixed exception on server returning string but not json
- [IRequestHandle] fixed unnecessary retry on HttpRequestException
- [Project Settings] fixed allocation issue on returning combined headers
### Modified
- [IRequestHandle] all body with Delete method

## [2.0.5] 2025-11-08
### Modified
- [Project Settings] API Sections changed to be Profiles with domain and headers
- [API] get default headers or specified section headers

## [2.0.2] - 2025-11-08
### Fixed
- [Project Settings] fix api section not shown issue
### Modified
- [Project Settings] manual input http:// or https:// in api section
- [IRequestHandle] simplify body builder

## [2.0.1] - 2025-09-25
### Fixed
- dependency issue

## [2.0.0] - 2025-09-23
### Added
- [IRequestHandle] throw different exception types
### Modified
- [Dependency] TaskUtils v2, for catch different exception types

---

## [1.0.11] - 2025-09-23
### Modified
- [Dependency] Library 2.0.3 for data models
- [Editor] use CsvData

## [1.0.10] - 2025-03-22
### Add
- [Project Settings] Auto Fill Headers for default headers on web request, can be modified on creating request
- [Request Handle] Task<T> Send<T>() will convert received JToken to given type
- [Request Builder] WithHeader on Get() and Delete() request

## [1.0.9] - 2025-01-25
### Modify
- [Editor] AceLand Project Setting as Tree structure

## [1.0.6] - 2024-11-26
### Modify
- [Editor] Undo functional for Project Settings

## [1.0.4] - 2024-11-24
First public release. If you have an older version, please update or re-install.   
For detail please visit and bookmark our [GitBook](https://aceland-workshop.gitbook.io/aceland-unity-packages/)[package.json](package.json)[package.json](package.json)