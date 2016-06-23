cd R:\Kraken\References\DSO\64_NET40
R:
COPY Interop.DSOFile.dll Interop.DSOFile.dll.bak /Y
SN -k Interop.DSOFile.snk
ILDASM Interop.DSOFile.dll /out:Interop.DSOFile.il
ILASM Interop.DSOFile.il /dll /resource=Interop.DSOFile.res /key=Interop.DSOFile.snk
pause
