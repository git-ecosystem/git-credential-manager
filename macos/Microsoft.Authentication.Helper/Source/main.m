// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#import <Foundation/Foundation.h>
#import "NSMutableDictionary+DictionaryFromConfig.h"
#import "AHGenerateAccessToken.h"
#import "AHLogger.h"
#import "AHFileLogWriter.h"
#import "AHStdErrorLogWriter.h"

BOOL isTruthy(NSString* value) {
    if (value == nil) {
        return false;
    }

    return ([value caseInsensitiveCompare:@"1"] == NSOrderedSame) ||
    ([value caseInsensitiveCompare:@"yes"] == NSOrderedSame) ||
    ([value caseInsensitiveCompare:@"true"] == NSOrderedSame);
}

BOOL isLocalFilePath(NSString *path) {
    NSString *fullpath = path.stringByExpandingTildeInPath;
    return [fullpath hasPrefix:@"/"];
}

int main(int argc, const char * argv[]) {
    @autoreleasepool {
        NSError* error;

        AHLogger *logger = [[AHLogger alloc] init];

        NSString* traceEnvar = [[[NSProcessInfo processInfo] environment] objectForKey:@"GCM_TRACE"];

        if (isTruthy(traceEnvar)) {
            [logger addWriter:[[AHStdErrorLogWriter alloc] init]];
        }
        else if (isLocalFilePath(traceEnvar)) {
            [logger addWriter:[[AHFileLogWriter alloc] initWithPath:traceEnvar]];
        }

        [logger log:@"Running Microsoft Authentication helper for macOS"];

        NSMutableDictionary<NSString *, NSString *> *configs = [NSMutableDictionary dictionaryFromFileHandle:[NSFileHandle fileHandleWithStandardInput]];

        // Extract expected parameters from input
        NSString* authority = [configs objectForKey:@"authority"];
        NSString* clientId = [configs objectForKey:@"clientId"];
        NSString* resource = [configs objectForKey:@"resource"];
        NSString* redirectUri = [configs objectForKey:@"redirectUri"];

        NSString *accessToken = [AHGenerateAccessToken generateAccessTokenWithAuthority:authority
                                                                               clientId:clientId
                                                                               resource:resource
                                                                            redirectUri:redirectUri
                                                                                  error:&error
                                                                                 logger:logger];

        NSString* output = [NSString stringWithFormat:@"accessToken=%@\n", accessToken];
        [output writeToFile:@"/dev/stdout" atomically:NO encoding:NSUTF8StringEncoding error:&error];
    }

    return 0;
}
