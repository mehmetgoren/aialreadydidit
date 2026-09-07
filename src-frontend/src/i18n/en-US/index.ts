import common from './common'
import catalog from './catalog'
import dashboard from './dashboard'
import upload from './upload'
import admin from './admin'

/** Flat snake_case keys (prototype convention). Domain files are merged; keys must be unique. */
export default {
  ...common,
  ...catalog,
  ...dashboard,
  ...upload,
  ...admin,
}
