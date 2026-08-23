import { View, StyleSheet } from 'react-native';
import { Tabs } from 'expo-router';
import { useTranslation } from 'react-i18next';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useTheme } from '../../src/context/ThemeContext';
import { BackToMoreButton } from '../../src/components/BackToMoreButton';
import { HeaderNotificationIcon } from '../../src/components/HeaderNotificationIcon';
import { Icon, type IconName } from '../../src/components/Icon';

const tabIcons: Record<string, IconName> = {
  index: 'dashboard',
  expenses: 'expense',
  budget: 'budget',
  more: 'more',
};

export default function TabsLayout() {
  const { t } = useTranslation();
  const { colors } = useTheme();
  const insets = useSafeAreaInsets();
  return (
    <Tabs
      screenOptions={{
        headerStyle: { backgroundColor: colors.bg.default },
        headerTintColor: colors.text.primary,
        headerShadowVisible: false,
        headerRight: () => <HeaderNotificationIcon />,
        tabBarActiveTintColor: colors.brand,
        tabBarInactiveTintColor: colors.text.muted,
        // No sceneContainerStyle here: every screen already gets its top safe-area
        // handled either by its own native header (headerShown: true, or a nested
        // Stack under app/(tabs)/*/_layout.tsx) or, for the one screen with neither
        // (Dashboard), by its own manual `insets.top` padding. This prop isn't part
        // of this navigator's type at all — it was adding a redundant extra gap under
        // every header, most visible as blank space above Budget's content.
        tabBarStyle: {
          backgroundColor: colors.bg.default,
          borderTopColor: colors.border,
          paddingBottom: insets.bottom,
          height: 56 + insets.bottom,
        },
      }}
    >
      <Tabs.Screen
        name="index"
        options={{
          title: t('tabs.dashboard'),
          tabBarLabel: t('tabs.dashboard'),
          headerShown: false,
          tabBarIcon: ({ color, size }) => <TabIcon name={tabIcons.index} color={color} size={size} />,
        }}
      />
      <Tabs.Screen
        name="expenses"
        options={{
          title: t('tabs.expenses'),
          tabBarLabel: t('tabs.expenses'),
          headerShown: false,
          tabBarIcon: ({ color, size }) => <TabIcon name={tabIcons.expenses} color={color} size={size} />,
        }}
      />
      <Tabs.Screen
        name="budget"
        options={{
          title: t('tabs.budget'),
          tabBarLabel: t('tabs.budget'),
          headerShown: true,
          headerRight: () => <HeaderNotificationIcon />,
          tabBarIcon: ({ color, size }) => <TabIcon name={tabIcons.budget} color={color} size={size} />,
        }}
      />
      <Tabs.Screen
        name="more"
        options={{
          title: t('tabs.more'),
          tabBarLabel: t('tabs.more'),
          headerShown: true,
          headerLeft: () => (
            <View style={styles.headerLeftIcon}>
              <Icon name="more" size={22} color={colors.text.primary} />
            </View>
          ),
          headerRight: () => <HeaderNotificationIcon />,
          tabBarIcon: ({ color, size }) => <TabIcon name={tabIcons.more} color={color} size={size} />,
        }}
      />
      <Tabs.Screen
        name="profile"
        options={{ href: null, title: t('more.profile'), headerLeft: () => <BackToMoreButton /> }}
      />
      <Tabs.Screen name="categories" options={{ href: null, headerShown: false }} />
      <Tabs.Screen
        name="privacy"
        options={{ href: null, title: t('more.privacyPolicy'), headerLeft: () => <BackToMoreButton /> }}
      />
      <Tabs.Screen
        name="terms"
        options={{ href: null, title: t('more.termsOfService'), headerLeft: () => <BackToMoreButton /> }}
      />
      <Tabs.Screen name="income" options={{ href: null, headerShown: false }} />
      <Tabs.Screen name="accounts" options={{ href: null, headerShown: false }} />
      <Tabs.Screen name="transactions" options={{ href: null, headerShown: false }} />
      <Tabs.Screen
        name="notifications"
        options={{ href: null, title: t('more.notifications'), headerLeft: () => <BackToMoreButton /> }}
      />
      <Tabs.Screen name="recurring" options={{ href: null, headerShown: false }} />
      <Tabs.Screen
        name="feedback"
        options={{ href: null, title: t('more.feedback'), headerLeft: () => <BackToMoreButton /> }}
      />
      <Tabs.Screen
        name="reports"
        options={{ href: null, title: t('more.monthlyReport'), headerLeft: () => <BackToMoreButton /> }}
      />
      <Tabs.Screen
        name="subscription"
        options={{ href: null, title: t('more.subscription'), headerLeft: () => <BackToMoreButton /> }}
      />
    </Tabs>
  );
}

const styles = StyleSheet.create({
  headerLeftIcon: { marginLeft: 8, padding: 8, justifyContent: 'center', alignItems: 'center' },
  headerIconText: { fontSize: 22 },
});

function TabIcon({ name, color, size }: { name: IconName; color: string; size: number }) {
  return (
    <View style={{ width: size, height: size, justifyContent: 'center', alignItems: 'center' }}>
      <Icon name={name} size={size} color={color} />
    </View>
  );
}
