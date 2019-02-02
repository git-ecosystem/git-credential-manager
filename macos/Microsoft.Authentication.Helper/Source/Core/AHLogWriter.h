// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

@protocol AHLogWriter <NSObject>

- (void)writeMessage:(NSString*)message;

@end


NS_ASSUME_NONNULL_END
