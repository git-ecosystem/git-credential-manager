// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#import <Foundation/Foundation.h>
#import "AHLogger.h"

NS_ASSUME_NONNULL_BEGIN

@interface AHGenerateAccessToken : NSObject
+ (NSString*) generateAccessTokenWithAuthority:(NSString*)authority
                                      clientId:(NSString*)clientId
                                      resource:(NSString*)resource
                                   redirectUri:(NSString*)redirectUri
                                         error:(NSError**)error
                                        logger:(AHLogger*)logger;
@end

NS_ASSUME_NONNULL_END
