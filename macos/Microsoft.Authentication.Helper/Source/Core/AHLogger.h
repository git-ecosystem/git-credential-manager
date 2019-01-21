// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#import <Foundation/Foundation.h>
#import "AHLogWriter.h"

NS_ASSUME_NONNULL_BEGIN

@interface AHLogger : NSObject
{
    NSMutableArray<AHLogWriter> *writers;
    NSDateFormatter *dateFormatter;
}

- (instancetype) init;
- (void) addWriter:(NSObject<AHLogWriter> *) writer;
- (void) log:(NSString*)message;

@end

NS_ASSUME_NONNULL_END
