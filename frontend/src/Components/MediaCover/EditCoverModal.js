import PropTypes from 'prop-types';
import React, { Component } from 'react';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Icon from 'Components/Icon';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { icons, kinds } from 'Helpers/Props';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import styles from './EditCoverModal.css';

class EditCoverModal extends Component {

  //
  // Lifecycle
  //

  constructor(props) {
    super(props);

    this.state = {
      activeTab: 'url', // 'url' | 'upload'
      imageUrl: '',
      selectedFile: null,
      previewUrl: null,
      isSaving: false,
      errorMessage: null
    };

    this.fileInputRef = React.createRef();
  }

  //
  // Listeners
  //

  onTabChange = (tab) => {
    this.setState({
      activeTab: tab,
      errorMessage: null
    });
  };

  onImageUrlChange = (e) => {
    this.setState({
      imageUrl: e.target.value,
      errorMessage: null
    });
  };

  onFileChange = (e) => {
    const file = e.target.files && e.target.files[0];
    if (file) {
      this.setState({
        selectedFile: file,
        previewUrl: URL.createObjectURL(file),
        errorMessage: null
      });
    }
  };

  onDrop = (e) => {
    e.preventDefault();
    const file = e.dataTransfer.files && e.dataTransfer.files[0];
    if (file) {
      this.setState({
        selectedFile: file,
        previewUrl: URL.createObjectURL(file),
        errorMessage: null
      });
    }
  };

  onDragOver = (e) => {
    e.preventDefault();
  };

  onSave = () => {
    const { type, id, onModalClose } = this.props;
    const { activeTab, imageUrl, selectedFile } = this.state;

    this.setState({ isSaving: true, errorMessage: null });

    let ajaxOptions;

    if (activeTab === 'url') {
      if (!imageUrl || !imageUrl.trim()) {
        this.setState({ isSaving: false, errorMessage: 'Please enter a valid image URL' });
        return;
      }

      ajaxOptions = {
        url: `/mediacover/${type}/${id}`,
        method: 'POST',
        dataType: 'json',
        data: JSON.stringify({ url: imageUrl.trim() })
      };
    } else {
      if (!selectedFile) {
        this.setState({ isSaving: false, errorMessage: 'Please select an image file to upload' });
        return;
      }

      const formData = new FormData();
      formData.append('file', selectedFile);

      ajaxOptions = {
        url: `/mediacover/${type}/${id}/upload`,
        method: 'POST',
        data: formData,
        processData: false,
        contentType: false
      };
    }

    const { request } = createAjaxRequest(ajaxOptions);

    request
      .done(() => {
        this.setState({ isSaving: false });
        onModalClose();
        // Reload to render new cover thumbnails
        window.location.reload();
      })
      .fail((xhr) => {
        let error = 'Failed to update cover image';
        if (xhr && xhr.responseText) {
          try {
            const parsed = JSON.parse(xhr.responseText);
            error = parsed.message || parsed.title || error;
          } catch (e) {
            error = xhr.responseText;
          }
        }
        this.setState({ isSaving: false, errorMessage: error });
      });
  };

  //
  // Render
  //

  render() {
    const { isOpen, onModalClose, type, title } = this.props;
    const { activeTab, imageUrl, selectedFile, previewUrl, isSaving, errorMessage } = this.state;

    return (
      <Modal isOpen={isOpen} onModalClose={onModalClose}>
        <ModalContent>
          <ModalHeader>
            {`Change ${type === 'author' ? 'Author' : 'Book'} Cover - ${title}`}
          </ModalHeader>

          <ModalBody>
            <div className={styles.tabButtons}>
              <button
                type="button"
                className={`${styles.tabButton} ${activeTab === 'url' ? styles.activeTab : ''}`}
                onClick={() => this.onTabChange('url')}
              >
                <Icon name={icons.EXTERNAL_LINK} /> Image URL
              </button>
              <button
                type="button"
                className={`${styles.tabButton} ${activeTab === 'upload' ? styles.activeTab : ''}`}
                onClick={() => this.onTabChange('upload')}
              >
                <Icon name={icons.FILEIMPORT} /> Upload File
              </button>
            </div>

            {errorMessage && (
              <div className={styles.errorAlert}>
                <Icon name={icons.WARNING} /> {errorMessage}
              </div>
            )}

            {activeTab === 'url' && (
              <FormGroup>
                <FormLabel>Cover Image URL</FormLabel>
                <FormInputGroup>
                  <input
                    type="text"
                    className={styles.textInput}
                    placeholder="https://example.com/cover.jpg"
                    value={imageUrl}
                    onChange={this.onImageUrlChange}
                    autoFocus
                  />
                </FormInputGroup>
                <div className={styles.helpText}>
                  Paste a direct link to a JPG, PNG, or WebP image. Readarr will fetch and resize it automatically.
                </div>
              </FormGroup>
            )}

            {activeTab === 'upload' && (
              <FormGroup>
                <FormLabel>Upload Image File</FormLabel>
                <div
                  className={styles.dropZone}
                  onDrop={this.onDrop}
                  onDragOver={this.onDragOver}
                  onClick={() => this.fileInputRef.current && this.fileInputRef.current.click()}
                >
                  <input
                    type="file"
                    ref={this.fileInputRef}
                    style={{ display: 'none' }}
                    accept="image/jpeg,image/png,image/gif,image/webp"
                    onChange={this.onFileChange}
                  />
                  {previewUrl ? (
                    <div className={styles.previewContainer}>
                      <img src={previewUrl} alt="Preview" className={styles.previewImage} />
                      <div className={styles.previewFilename}>{selectedFile && selectedFile.name}</div>
                    </div>
                  ) : (
                    <div className={styles.dropZonePrompt}>
                      <Icon name={icons.FILEIMPORT} className={styles.uploadIcon} />
                      <div>Click to browse or drag and drop an image here</div>
                      <div className={styles.dropZoneSubtext}>Supports JPG, PNG, GIF, WebP</div>
                    </div>
                  )}
                </div>
              </FormGroup>
            )}
          </ModalBody>

          <ModalFooter>
            <Button
              kind={kinds.DEFAULT}
              onPress={onModalClose}
              isDisabled={isSaving}
            >
              Cancel
            </Button>
            <SpinnerButton
              kind={kinds.PRIMARY}
              isSpinning={isSaving}
              onPress={this.onSave}
            >
              Save Cover
            </SpinnerButton>
          </ModalFooter>
        </ModalContent>
      </Modal>
    );
  }
}

EditCoverModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired,
  type: PropTypes.oneOf(['author', 'book']).isRequired,
  id: PropTypes.number.isRequired,
  title: PropTypes.string.isRequired
};

export default EditCoverModal;
