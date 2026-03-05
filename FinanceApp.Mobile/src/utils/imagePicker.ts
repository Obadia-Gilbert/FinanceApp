import * as ImagePicker from 'expo-image-picker';
import { Alert, Linking } from 'react-native';

export interface PickedImage {
  uri: string;
  fileName: string;
  mimeType: string;
}

/**
 * Request camera permission and return whether granted.
 */
async function requestCameraPermission(): Promise<boolean> {
  const { status } = await ImagePicker.requestCameraPermissionsAsync();
  return status === 'granted';
}

/**
 * Request media library permission and return whether granted.
 */
async function requestMediaLibraryPermission(): Promise<boolean> {
  const { status } = await ImagePicker.requestMediaLibraryPermissionsAsync();
  return status === 'granted';
}

/**
 * Pick an image from the device (camera or gallery). Returns asset uri and inferred filename/mime.
 * Use this when creating expense/income to attach a receipt or supporting document.
 */
export async function pickSupportingDocument(): Promise<PickedImage | null> {
  return new Promise((resolve) => {
    Alert.alert(
      'Attach document',
      'Take a photo or choose from library',
      [
        {
          text: 'Take photo',
          onPress: async () => {
            const granted = await requestCameraPermission();
            if (!granted) {
              Alert.alert(
                'Permission needed',
                'Camera access is required to take a photo of your receipt.',
                [{ text: 'OK' }, { text: 'Open Settings', onPress: () => Linking.openSettings() }]
              );
              resolve(null);
              return;
            }
            const result = await ImagePicker.launchCameraAsync({
              mediaTypes: ImagePicker.MediaTypeOptions.Images,
              allowsEditing: true,
              aspect: [4, 3],
              quality: 0.8,
            });
            if (result.canceled) {
              resolve(null);
              return;
            }
            const asset = result.assets[0];
            const uri = asset.uri;
            const fileName = asset.fileName ?? `receipt_${Date.now()}.jpg`;
            const mimeType = asset.mimeType ?? 'image/jpeg';
            resolve({ uri, fileName, mimeType });
          },
        },
        {
          text: 'Choose from library',
          onPress: async () => {
            const granted = await requestMediaLibraryPermission();
            if (!granted) {
              Alert.alert(
                'Permission needed',
                'Photo library access is required to attach a document.',
                [{ text: 'OK' }, { text: 'Open Settings', onPress: () => Linking.openSettings() }]
              );
              resolve(null);
              return;
            }
            const result = await ImagePicker.launchImageLibraryAsync({
              mediaTypes: ImagePicker.MediaTypeOptions.Images,
              allowsEditing: true,
              aspect: [4, 3],
              quality: 0.8,
            });
            if (result.canceled) {
              resolve(null);
              return;
            }
            const asset = result.assets[0];
            const uri = asset.uri;
            const fileName = asset.fileName ?? `document_${Date.now()}.jpg`;
            const mimeType = asset.mimeType ?? 'image/jpeg';
            resolve({ uri, fileName, mimeType });
          },
        },
        { text: 'Cancel', style: 'cancel', onPress: () => resolve(null) },
      ],
      { cancelable: true }
    );
  });
}
