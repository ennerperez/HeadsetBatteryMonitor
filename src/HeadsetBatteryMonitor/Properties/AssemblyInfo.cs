using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("Headset Battery Monitor")]
[assembly: AssemblyDescription("Headset Battery Monitor")]
#if DEBUG
[assembly: AssemblyConfiguration("DEBUG")]
#else
[assembly: AssemblyConfiguration("RELEASE")]
#endif
[assembly: AssemblyCompany("Enner Pérez")]
[assembly: AssemblyProduct("Headset Battery Monitor")]
[assembly: AssemblyCopyright("Copyright © Enner Pérez")]
[assembly: AssemblyTrademark("Headset Battery Monitor")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(true)]
[assembly: Guid("B46D1000-FFFD-4775-BAD2-B1181860F9B3")]
[assembly: AssemblyVersion("1.0.0.*")]
[assembly: AssemblyFileVersion("1.0.0.*")]
[assembly: NeutralResourcesLanguage("en")]
[assembly: GitHub("ennerperez", "HeadsetBatteryMonitor")]

[assembly:SuppressMessage("Interoperability", "CA1416", MessageId = "Invalid Platform")]
