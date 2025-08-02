/**
 * 🏺 Ritual Scroll: badgeEmitter.ts
 * Description: Listens for sponsorPing events, logs relic drops, and broadcasts
 *              badge-ceremony embeds across configured Discord channels.
 */

import { Client, GatewayIntentBits, TextChannel, ThreadChannel, ChannelType } from 'discord.js';
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
  // 1️⃣ Load & validate badge-locations.yml
  const cfgPath = path.resolve(__dirname, '../config/badge-locations.yml');
  let config: Config = {};

  try {
    const raw = await fs.readFile(cfgPath, 'utf8');
    config = (yaml.load(raw) as Config) ?? {};
  } catch (err) {
    console.error('❌ Failed to load badge-locations.yml:', err);
    process.exit(1);
  }

  // 2️⃣ Initialize clients
  const badgeClient = new BadgeClient({ apiToken: process.env.BADGE_API_TOKEN! });
  const discord    = new Client({ intents: [GatewayIntentBits.Guilds] });

  discord.once('ready', () => {
    console.log(`✅ Discord bot ready as ${discord.user?.tag}`);
  });

  // 3️⃣ Handle sponsorPing events
  badgeClient.on('sponsorPing', async ({ sponsorId, badgeName }: SponsorPingPayload) => {
    const channels  = config[badgeName]?.drop_channels ?? [];
    const timestamp = new Date().toISOString();
    const threadId  = `thread-${timestamp.replace(/[:.]/g, '-')}`;
    const relic: Relic = { sponsorId, badgeName, timestamp, threadId };

    // 3a️⃣ Audit-log the relic
    const logDir  = path.resolve(__dirname, '../threads');
    await fs.mkdir(logDir, { recursive: true });
    const fileName = `relic-drop--${badgeName}--${timestamp}.json`;
    const outPath  = path.join(logDir, fileName);
    await fs.writeFile(outPath, JSON.stringify(relic, null, 2));

    // 3b️⃣ Broadcast the badge ceremony
    for (const chId of channels) {
      try {
        const channel = await discord.channels.fetch(chId);
        if (
          channel &&
          (channel.type === ChannelType.GuildText ||
           channel.type === ChannelType.GuildPublicThread)
        ) {
          await dropBadge(channel as TextChannel | ThreadChannel, {
            ...relic,
            title: `🏅 ${badgeName}`,
          });
        } else {
          console.warn(`⚠️ Skipping non-text/thread channel: ${chId}`);
        }
      } catch (err) {
        console.error(`❌ Error dropping badge in ${chId}:`, err);
      }
    }
  });

  // 4️⃣ Connect to Discord
  await discord.login(process.env.DISCORD_TOKEN);
})();
