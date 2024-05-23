const path = require('path');
const CopyPlugin = require('copy-webpack-plugin');

module.exports = {
  entry: './wwwroot/js/site.js', // Path to your main JS file
  output: {
    path: path.resolve(__dirname, 'wwwroot/js'), // Output directory
    filename: 'bundle.js', // Output bundle file name
  },
  plugins: [
    new CopyPlugin({
      patterns: [
        { from: 'node_modules/moment/min/moment.min.js', to: 'moment.min.js' },
        { from: 'node_modules/htmx.org/dist/htmx.min.js', to: 'htmx.min.js' },
        { from: 'node_modules/prismjs/prism.js', to: 'prism.js' },
        { from: 'node_modules/prismjs/components/prism-json.min.js', to: 'prism-json.min.js' },
        { from: 'node_modules/@microsoft/signalr/dist/browser/signalr.min.js', to: 'signalr.min.js' },
        { from: 'node_modules/marked/marked.min.js', to: 'marked.min.js' },
      ],
    }),
  ],
};