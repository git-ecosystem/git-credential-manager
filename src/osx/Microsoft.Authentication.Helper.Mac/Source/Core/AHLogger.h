// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#import <Foundation/Foundation.h>
#import "AHLogWriter.h"

#define AHLOG(LOGGER,MSG) [LOGGER log:(MSG) fileName:@__FILE__ lineNum:__LINE__]
#define AHLOG_SECRET(LOGGER,MSG,SECRET) [LOGGER log:(MSG) secretMsg:(SECRET) fileName:@__FILE__ lineNum:__LINE__]

NS_ASSUME_NONNULL_BEGIN

@interface AHLogger : NSObject
{
    NSMutableArray<AHLogWriter> *writers;
    NSDateFormatter *dateFormatter;
}

@property BOOL enableSecretTracing;

- (instancetype) init;
- (void) addWriter:(NSObject<AHLogWriter> *) writer;
- (void) log:(NSString*)message
    fileName:(NSString*)fileName
     lineNum:(int)lineNum;
- (void) log:(NSString*)message
   secretMsg:(NSString*)secretMsg
    fileName:(NSString*)fileName
    lineNum:(int)lineNum;

@end

NS_ASSUME_NONNULL_END
