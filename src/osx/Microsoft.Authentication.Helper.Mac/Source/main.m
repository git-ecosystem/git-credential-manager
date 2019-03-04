// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#import <Foundation/Foundation.h>
#import "NSMutableDictionary+DictionaryFromConfig.h"
#import "AHGenerateAccessToken.h"
#import "AHLogger.h"
#import "AHFileLogWriter.h"
#import "AHStdErrorLogWriter.h"
#import <ADAL/ADAL.h>

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
        int exitCode;
        NSError* error;

        AHLogger *logger = [[AHLogger alloc] init];

        // Configure the application logger based on the common GCM tracing environment variables
        NSString* traceEnvar        = [[[NSProcessInfo processInfo] environment] objectForKey:@"GCM_TRACE"];
        NSString* traceSecretsEnvar = [[[NSProcessInfo processInfo] environment] objectForKey:@"GCM_TRACE_SECRETS"];
        NSString* traceMsAuthEnvar  = [[[NSProcessInfo processInfo] environment] objectForKey:@"GCM_TRACE_MSAUTH"];

        // Enable tracing
        if (isTruthy(traceEnvar)) {
            [logger addWriter:[[AHStdErrorLogWriter alloc] init]];
        }
        else if (isLocalFilePath(traceEnvar)) {
            [logger addWriter:[[AHFileLogWriter alloc] initWithPath:traceEnvar]];
        }

        // Enable tracing of secret/sensitive information
        if (isTruthy(traceSecretsEnvar)) {
            [logger setEnableSecretTracing:YES];

            // Also enable PII logging in ADAL
            [ADLogger setPiiEnabled:YES];
        }

        // Always disable ADAL from logging to NSLog which prints to stderror directly
        [ADLogger setNSLogging:NO];

        // Register a callback in ADAL for our logger if GCM_TRACE_MSAUTH is enabled
        if (isTruthy(traceMsAuthEnvar))
        {
            // We try and capture everything we can as this can help with diagnosing
            // the most complicated authentication problems.
            [ADLogger setLevel:ADAL_LOG_LEVEL_VERBOSE];

            [ADLogger setLoggerCallback:^(ADAL_LOG_LEVEL logLevel, NSString *message, BOOL containsPii) {

                NSString* logLevelName;
                switch (logLevel) {
                    case ADAL_LOG_LEVEL_ERROR:
                        logLevelName = @"Error";
                        break;
                    case ADAL_LOG_LEVEL_WARN:
                        logLevelName = @"Warning";
                        break;
                    case ADAL_LOG_LEVEL_INFO:
                        logLevelName = @"Info";
                        break;
                    case ADAL_LOG_LEVEL_VERBOSE:
                        logLevelName = @"Verbose";
                        break;
                    default:
                        logLevelName = @"Unknown";
                        break;
                }

                [logger log:[NSString stringWithFormat:@"[ADAL] [%@] %@", logLevelName, message] fileName:@"ADAL" lineNum:0];
            }];
        }

        AHLOG(logger, @"Running Microsoft Authentication helper for macOS");

        NSMutableDictionary<NSString *, NSString *> *configs = [NSMutableDictionary dictionaryFromFileHandle:[NSFileHandle fileHandleWithStandardInput]];

        // Extract expected parameters from input
        NSString* authority   = [configs objectForKey:@"authority"];
        NSString* clientId    = [configs objectForKey:@"clientId"];
        NSString* resource    = [configs objectForKey:@"resource"];
        NSString* redirectUri = [configs objectForKey:@"redirectUri"];

        NSString *accessToken = [AHGenerateAccessToken generateAccessTokenWithAuthority:authority
                                                                               clientId:clientId
                                                                               resource:resource
                                                                            redirectUri:redirectUri
                                                                                  error:&error
                                                                                 logger:logger];

        NSString* output;

        if (error == nil && accessToken != nil)
        {
            output = [NSString stringWithFormat:@"accessToken=%@\n", accessToken];
            exitCode = 0;
        }
        else
        {
            output = [NSString stringWithFormat:@"error=%@\n", [error description]];
            exitCode = -1;
        }

        [output writeToFile:@"/dev/stdout" atomically:NO encoding:NSUTF8StringEncoding error:&error];
        return exitCode;
    }
}
