# Are you working on the command line?
# Do you love unix tools?
# Then rejoice! You can type 'make test' to build and test!

all: build

build: FORCE
	bin/build

clean:
	rm -rf AutoUpdate/bin  AutoUpdate/obj \
	       Cmdline/bin     Cmdline/obj \
	       Core/bin        Core/obj \
	       GUI/bin         GUI/obj \
	       CFAN-netfan/bin CFAN-netfan/obj \
	       Tests/bin       Tests/obj \
	       cfan.exe        cfan.exe.mdb \
	       netfan.exe      netfan.exe.mdb

test: build
	nunit-console --exclude=FlakyNetwork build/Tests/bin/Debug/CKAN.Tests.dll

# This likely only works under Unix, but then again Unix folks are the only
# ones likely to have `fortune` installed. This target is for amusement purposes
# only.
fortune: quotes.txt
	strfile quotes.txt

FORCE:
