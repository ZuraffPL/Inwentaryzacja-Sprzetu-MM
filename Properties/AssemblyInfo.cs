using System.Reflection;
using System.Runtime.Versioning;
using System.Windows;

// Wersja assembly — wymagana przez WPF do rozpoznawania zasobów BAML (Pack URI).
// Ten plik istnieje ponieważ GenerateAssemblyInfo=false wyłącza auto-generowanie,
// co jest konieczne aby uniknąć duplikatów atrybutów z projektem _wpftmp (WPF SDK).
// WAŻNE: wersja MUSI być zgodna z <AssemblyVersion> w .csproj (pack URI musi pasować do wersji assembly).
[assembly: AssemblyVersion("1.9.4.0")]
[assembly: AssemblyFileVersion("1.9.4.0")]
[assembly: AssemblyInformationalVersion("1.9.4")]
[assembly: AssemblyTitle("Inwentaryzacja Sprzętu")]
[assembly: AssemblyProduct("Inwentaryzacja Sprzętu")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyConfiguration("")]
[assembly: TargetPlatform("Windows")]
[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]
