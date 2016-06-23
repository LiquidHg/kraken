cd R:\Kraken\References\DSO\64_NET35
R:
COPY Interop.DSOFile.dll Interop.DSOFile.dll.bak /Y
SN -k Interop.DSOFile.snk
ILDASM Interop.DSOFile.dll /out:Interop.DSOFile.il
C:\Windows\Microsoft.NET\Framework\v2.0.50727\ILASM Interop.DSOFile.il /dll /resource=Interop.DSOFile.res /key=Interop.DSOFile.snk
pause
