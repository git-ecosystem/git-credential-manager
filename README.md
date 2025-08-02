import {
  Client,
  GatewayIntentBits,
  TextChannel,
  ThreadChannel,
  ChannelType
} from 'discord.js';
import { BadgeClient } from 'kypria-badge-sdk';
import { promises as fs } from 'fs';
import path from 'path';
import yaml from 'js-yaml';
import { dropBadge } from './utils/dropBadge';

type BadgeConfig = {
  drop_channels: string[];
};

type Config = Record<string, BadgeConfig>;

type SponsorPingPayload = {
  sponsorId: string;
  badgeName: string;
};

type Relic = SponsorPingPayload & {
  timestamp: string;
  threadId: string;
};

(async () => {
  // Load badge config
  const cfgPath = path.resolve(__dirname, '../config/badge-locations.yml');
  let config: Config = {};
  try {
    const raw = await fs.readFile(cfgPath, 'utf8');
    config = (yaml.load(raw) as Config) ?? {};
  } catch (err) {
    console.error('❌ badge-locations.yml failed to load:', err);
    process.exit(1);
  }

  // Initialize clients
  const badgeClient = new BadgeClient({ apiToken: process.env.BADGE_API_TOKEN! });
  const discord = new Client({ intents: [GatewayIntentBits.Guilds] });

  discord.once('ready', () => {
    console.log(`✅ Discord bot logged in as ${discord.user?.tag}`);
  });

  // Handle sponsorPing
  badgeClient.on('sponsorPing', async ({ sponsorId, badgeName }: SponsorPingPayload) => {
    const channels = config[badgeName]?.drop_channels ?? [];
    const timestamp = new Date().toISOString();
    const threadId = `thread-${timestamp.replace(/[:.]/g, '-')}`;
    const relic: Relic = { sponsorId, badgeName, timestamp, threadId };

    // Write audit log
    const logDir = path.resolve(__dirname, '../threads');
    await fs.mkdir(logDir, { recursive: true });
    const logPath = path.join(logDir, `relic-drop--${badgeName}--${timestamp}.json`);
    await fs.writeFile(logPath, JSON.stringify(relic, null, 2));

    // Broadcast drop
    for (const chId of channels) {
      try {
        const channel = await discord.channels.fetch(chId);
        if (
          channel &&
          (channel.type === ChannelType.GuildText || channel.type === ChannelType.GuildPublicThread)
        ) {
          await dropBadge(channel as TextChannel | ThreadChannel, {
            ...relic,
            title: `🏅 ${badgeName}`
          });
        } else {
          console.warn(`⚠️ Skipped invalid channel: ${chId}`);
        }
      } catch (err) {
        console.error(`❌ Drop failed in channel ${chId}:`, err);
      }
    }
  });

  await discord.login(process.env.DISCORD_TOKEN);
})();
