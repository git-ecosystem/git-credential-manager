// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#import "AHGenerateAccessToken.h"
#import <ADAL/ADAL.h>

#import "AHAppDelegate.h"

@implementation AHGenerateAccessToken

+ (NSString*) generateAccessTokenWithAuthority:(NSString*)authority
                                      clientId:(NSString*)clientId
                                      resource:(NSString*)resource
                                   redirectUri:(NSString*)redirectUri
                                         error:(NSError**)error
                                        logger:(AHLogger*)logger
{
    NSString* userId = @"";
    NSError* localError = nil;
    __block NSString* accessToken = nil;
    __block AHAppDelegate *appDelegate;

    AHAppWorkBlock workBlock = ^{
        ADAuthenticationError* error = nil;
        ADAuthenticationContext *context = [ADAuthenticationContext authenticationContextWithAuthority:authority
                                                                                     validateAuthority:NO
                                                                                                 error:&error];

        ADAuthenticationCallback completionBlock = ^(ADAuthenticationResult *result)
        {
            accessToken = result.accessToken;
            [appDelegate stop];
        };

        [context acquireTokenWithResource:resource
                                 clientId:clientId
                              redirectUri:[NSURL URLWithString:redirectUri]
                           promptBehavior: AD_PROMPT_ALWAYS
                           userIdentifier:[ADUserIdentifier identifierWithId:userId
                                                                        type:OptionalDisplayableId]
                     extraQueryParameters:nil
                          completionBlock:completionBlock];
    };

    appDelegate = [[AHAppDelegate alloc] initWithBlock:workBlock logger:logger];
    [appDelegate run];
    localError = [appDelegate error];

    if (localError && error)
    {
        *error = localError;
    }

    return accessToken;
}

@end
